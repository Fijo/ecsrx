using System;
using System.Collections.Generic;
using System.Reflection;
using SystemsRx.Attributes;
using SystemsRx.Executor.Handlers;
using SystemsRx.Extensions;
using SystemsRx.Systems;
using EcsRx.Collections;
using EcsRx.Entities;
using EcsRx.Extensions;
using EcsRx.Groups;
using EcsRx.MicroRx.Disposables;
using EcsRx.MicroRx.Extensions;
using EcsRx.Plugins.ReactiveSystems.Extensions;
using EcsRx.Plugins.ReactiveSystems.Systems;
using EcsRx.Systems;

namespace EcsRx.Plugins.ReactiveSystems.Handlers
{
    [Priority(3)]
    public class ReactToDataSystemHandler : IConventionalSystemHandler
    {
        public readonly IDictionary<ISystem, IDisposable> _systemSubscriptions;
        public readonly IDictionary<ISystem, IDictionary<int, IDisposable>> _entitySubscriptions;
        public readonly IObservableGroupManager ObservableGroupManager;

        private readonly MethodInfo _processEntityMethod;
        
        public ReactToDataSystemHandler(IObservableGroupManager observableGroupManager)
        {
            ObservableGroupManager = observableGroupManager;
            _systemSubscriptions = new Dictionary<ISystem, IDisposable>();
            _entitySubscriptions = new Dictionary<ISystem, IDictionary<int, IDisposable>>();
            _processEntityMethod = GetType().GetMethod("ProcessEntity");
        }

        // TODO: This is REALLY bad but currently no other way around the dynamic invocation lookup stuff
        public Func<IEntity, IDisposable> CreateEntityProcessorFunction(ISystem system)
        {
            var genericDataType = system.GetGenericDataType(typeof(IReactToDataSystem<>));
            var genericMethod = _processEntityMethod.MakeGenericMethod(genericDataType);
            return entity => (IDisposable) genericMethod.Invoke(this, new object[] {system, entity});
        }

        public IDisposable ProcessEntity<T>(IReactToDataSystem<T> system, IEntity entity)
        {
            var hasEntityPredicate = system.Group is IHasPredicate;
            var reactObservable = system.ReactToData(entity);
            
            if (!hasEntityPredicate)
            { return reactObservable.Subscribe(x => system.Process(entity, x)); }
            
            var groupPredicate = system.Group as IHasPredicate;
            return reactObservable
                .Subscribe(x =>
                {
                    if(groupPredicate.CanProcessEntity(entity))
                    { system.Process(entity, x);}
                });
        }
        
        public bool CanHandleSystem(ISystem system)
        { return system.IsReactiveDataSystem(); }

        public void SetupSystem(ISystem system)
        {
            var processEntityFunction = CreateEntityProcessorFunction(system);

            var entityChangeSubscriptions = new CompositeDisposable();
            _systemSubscriptions.Add(system, entityChangeSubscriptions);
            var entitySubscriptions = new Dictionary<int, IDisposable>();
            _entitySubscriptions.Add(system, entitySubscriptions);

            var groupSystem = system as IGroupSystem;
            var affinities = groupSystem.GetGroupAffinities();
            var observableGroup = ObservableGroupManager.GetObservableGroup(groupSystem.Group, affinities);

            observableGroup.OnEntityAdded
                .Subscribe(x =>
                {
                    var subscription = processEntityFunction(x);
                    entitySubscriptions.Add(x.Id, subscription);
                })
                .AddTo(entityChangeSubscriptions);
            
            observableGroup.OnEntityRemoved
                .Subscribe(x =>
                {
                    entitySubscriptions.RemoveAndDispose(x.Id);
                })
                .AddTo(entityChangeSubscriptions);

            foreach (var entity in observableGroup)
            {
                var subscription = processEntityFunction(entity);
                entitySubscriptions.Add(entity.Id, subscription);
            }
        }

        public void DestroySystem(ISystem system)
        {
            _systemSubscriptions.RemoveAndDispose(system);
            
            var entitySubscriptions = _entitySubscriptions[system];
            entitySubscriptions.Values.DisposeAll();
            entitySubscriptions.Clear();
            _entitySubscriptions.Remove(system);
        }
        
        public void Dispose()
        {
            _systemSubscriptions.DisposeAll();
            foreach (var entitySubscriptions in _entitySubscriptions.Values)
            {
                entitySubscriptions.Values.DisposeAll();
                entitySubscriptions.Clear();
            }
            _entitySubscriptions.Clear();
        }
    }
}