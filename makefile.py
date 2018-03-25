import os
import dotnet
import subprocess

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
    # See https://github.com/Microsoft/vstest/issues/1155
    # See https://github.com/Microsoft/vstest/issues/326
    dotnet_cli(
        'msbuild',
        bjoin('src', project_name + '.sln'),
        '/t:VSTest',
        '/p:Configuration=' + build_configuration,
        '/p:Platform=' + build_platform,
        '/p:Version=' + nuget_version
    )


def nuget_push():
    dotnet.nuget_push(bjoin(project_name + '.' + nuget_version + '.nupkg'))


def bjoin(*args):
    base_path = os.path.dirname(os.path.realpath(__file__))
    return os.path.join(base_path, *args)

def dotnet_cli(*args):
    args = list(args)
    args.insert(0, 'dotnet')
    subprocess.check_call(args)
