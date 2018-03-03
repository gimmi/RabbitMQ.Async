import os
import dotnet
import codecs

build_configuration = 'Release'
build_platform = 'Any CPU'
project_version = '0.3.0'
prerelease = True
build_vcs_number = 'n/a'
project_name = 'RabbitMQ.Async'
project_authors = 'Gherardi Gian Marco'
target_framework_moniker = 'net451'

default = ['build_props']
build = ['build_props', 'compile', 'test', 'nuget_pack']
release = ['build', 'nuget_push']


def build_props():
    dotnet.msbuild_props(bjoin('src', 'Directory.Build.props'),
        # Properties for AssemblyInfo
        Product=project_name,
        Copyright=project_authors,
        Company=project_authors,
        AssemblyVersion=project_version + '.0',
        FileVersion=project_version + '.0',
        InformationalVersion=nuget_version(),

        # Properties for NuGet package
        # PackageId=project_name,
        PackageVersion=nuget_version(),
        Authors=project_authors,
        PackageDescription='RabbitMQ.Async is a thin wrapper over the official RabbitMQ.Client library that provide integration with Microsoft TPL',
        PackageLicenseUrl='https://raw.githubusercontent.com/gimmi/RabbitMQ.Async/master/LICENSE',
        PackageProjectUrl='https://github.com/gimmi/RabbitMQ.Async',
        PackageTags='RabbitMQ Client AMQP TPL Task Parallel Message Bus Event',
        PackageIconUrl='https://raw.githubusercontent.com/gimmi/RabbitMQ.Async/master/icon.png'
    )


def compile():
    dotnet.msbuild(bjoin('src', project_name + '.sln'), 'Restore', 'Rebuild', Configuration=build_configuration, Platform=build_platform)


def test():
    dotnet.nunit('src/*/bin/{cfg}/{tfm}/*.Tests.dll'.format(
        cfg=build_configuration,
        tfm=target_framework_moniker
    ))


def nuget_pack():
    dotnet.msbuild(bjoin('src', project_name + '.sln'), 'Pack', Configuration=build_configuration, Platform=build_platform)


def nuget_push():
    dotnet.nuget_push(bjoin(project_name + '.' + nuget_version() + '.nupkg'))


def nuget_version():
    ret = project_version
    if prerelease:
        ret += '-pre'
    return ret


def bjoin(*args):
    base_path = os.path.dirname(os.path.realpath(__file__))
    return os.path.join(base_path, *args)
