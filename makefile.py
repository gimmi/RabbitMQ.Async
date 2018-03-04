import os
import dotnet

build_configuration = 'Release'
build_platform = 'Any CPU'
nuget_version = '0.3.0-pre'
project_name = 'RabbitMQ.Async'
target_framework_moniker = 'net451'

default = ['compile']
build = ['compile', 'test']
release = ['build', 'nuget_push']


def compile():
    dotnet.msbuild(bjoin('src', project_name + '.sln'), 'Restore', 'Rebuild', 'Pack',
        Configuration=build_configuration,
        Platform=build_platform,
        Version=nuget_version
    )


def test():
    dotnet.nunit('src/*/bin/{cfg}/{tfm}/*.Tests.dll'.format(
        cfg=build_configuration,
        tfm=target_framework_moniker
    ))


def nuget_push():
    dotnet.nuget_push(bjoin(project_name + '.' + nuget_version + '.nupkg'))


def bjoin(*args):
    base_path = os.path.dirname(os.path.realpath(__file__))
    return os.path.join(base_path, *args)
