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

default = ['init']
init = ['assembly_info']
build = ['init', 'compile', 'test', 'nuget_pack']
release = ['build', 'nuget_push']


def assembly_info():
    dotnet.msbuild_props(bjoin('src', 'Directory.Build.props'),
        Product=project_name,
        Copyright=project_authors,
        Company=project_authors,
        AssemblyVersion=project_version + '.0',
        FileVersion=project_version + '.0',
        InformationalVersion=nuget_version()
    )


def compile():
    dotnet.msbuild(bjoin('src', project_name + '.sln'), 'Restore', 'Rebuild', Configuration=build_configuration, Platform=build_platform)


def test():
    dotnet.nunit('src/*/bin/{cfg}/{tfm}/*.Tests.dll'.format(
        cfg=build_configuration,
        tfm=target_framework_moniker
    ))


def nuget_pack():
    nuspec_content = """\
<?xml version="1.0"?>
<package xmlns="http://schemas.microsoft.com/packaging/2010/07/nuspec.xsd">
    <metadata>
        <id>{id}</id>
        <version>{version}</version>
        <authors>{authors}</authors>
        <owners>{authors}</owners>
        <description>RabbitMQ.Async is a thin wrapper over the official RabbitMQ.Client library that provide integration with Microsoft TPL</description>
        <licenseUrl>https://raw.githubusercontent.com/gimmi/RabbitMQ.Async/master/LICENSE</licenseUrl>
        <projectUrl>https://github.com/gimmi/RabbitMQ.Async</projectUrl>
        <tags>RabbitMQ Client AMQP TPL Task Parallel Message Bus Event</tags>
        <iconUrl>https://raw.githubusercontent.com/gimmi/RabbitMQ.Async/master/icon.png</iconUrl>
        <dependencies>
            <dependency id="RabbitMQ.Client" version="5.0.1" />
        </dependencies>
    </metadata>
    <files>
        <file src="{id}\\bin\\{cfg}\\{id}.*" target="lib\\{tfm}" />
        <file src="{id}\\**\\*.cs" target="src" />
    </files>
</package>
    """.format(id=project_name, version=nuget_version(), authors=project_authors, release_notes=dotnet.git_tfs_release_notes(bjoin('.git')), cfg=build_configuration, tfm=target_framework_moniker)

    nuspec_file = bjoin('src', 'Package.nuspec')

    with codecs.open(nuspec_file, 'w', 'utf-8') as f:
        f.write(nuspec_content)

    dotnet.nuget_pack(nuspec_file, '-Symbols', '-OutputDirectory', bjoin())


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
