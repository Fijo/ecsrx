set version=3.0.0
dotnet pack ../src/EcsRx.MicroRx -c Release -o ../../_dist /p:version=%version%
dotnet pack ../src/SystemsRx -c Release -o ../../_dist /p:version=%version%
dotnet pack ../src/SystemsRx.Infrastructure -c Release -o ../../_dist /p:version=%version%
dotnet pack ../src/EcsRx -c Release -o ../../_dist /p:version=%version%
dotnet pack ../src/EcsRx.Plugins.ReactiveSystems -c Release -o ../../_dist /p:version=%version%
dotnet pack ../src/EcsRx.Plugins.Views -c Release -o ../../_dist /p:version=%version%
dotnet pack ../src/EcsRx.Plugins.Computeds -c Release -o ../../_dist /p:version=%version%
dotnet pack ../src/EcsRx.Plugins.Batching -c Release -o ../../_dist /p:version=%version%
dotnet pack ../src/EcsRx.Infrastructure -c Release -o ../../_dist /p:version=%version%
dotnet pack ../src/EcsRx.Infrastructure.Ninject -c Release -o ../../_dist /p:version=%version%
dotnet pack ../src/EcsRx.ReactiveData -c Release -o ../../_dist /p:version=%version%