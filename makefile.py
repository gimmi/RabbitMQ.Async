import os
import dotnet
import codecs

build_configuration = 'Release'
build_platform = 'Any CPU'
project_version = '0.1.0'
prerelease = True
build_number = 0
build_vcs_number = 'n/a'
project_name = 'RabbitMQ.Async'
project_authors = 'Gherardi Gian Marco'
project_description = 'RabbitMQ.Async'

default = ['init']
init = ['install_deps', 'assembly_info']
build = ['init', 'compile', 'test', 'nuget_pack']
release = ['build', 'nuget_push']


def install_deps():
    dotnet.nuget_restore(bjoin('src', project_name + '.sln'))


def assembly_info():
    dotnet.assembly_info(
        bjoin('src', 'SharedAssemblyInfo.cs'),
        AssemblyProduct=project_name,
        AssemblyCopyright='',
        AssemblyTrademark='',
        AssemblyCompany=project_authors,
        AssemblyConfiguration='%s|%s' % (build_configuration, build_platform),
        AssemblyVersion=project_version + '.0',
        AssemblyFileVersion=project_version + '.0',
        AssemblyInformationalVersion=nuget_version()
    )


def compile():
    dotnet.msbuild(bjoin('src', project_name + '.sln'), 'Rebuild', Configuration=build_configuration, Platform=build_platform)


def test():
    dotnet.nunit('src/*/bin/' + build_configuration + '/*.Tests.dll')


def nuget_pack():
    nuspec_content = """\
<?xml version="1.0"?>
<package xmlns="http://schemas.microsoft.com/packaging/2010/07/nuspec.xsd">
    <metadata>
        <id>{id}</id>
        <version>{version}</version>
        <authors>{authors}</authors>
        <owners>{authors}</owners>
        <description>{description}</description>
        <dependencies>
            <dependency id="RabbitMQ.Client" version="3.4.0" />
        </dependencies>
    </metadata>
    <files>
        <file src="{id}\\bin\\{cfg}\\{id}.*" target="lib\\net40" />
        <file src="{id}\\**\\*.cs" target="src" />
    </files>
</package>
    """.format(id=project_name, version=nuget_version(), authors=project_authors, description=project_description, release_notes=dotnet.git_tfs_release_notes(bjoin('.git')), cfg=build_configuration)

    nuspec_file = bjoin('src', 'Package.nuspec')

    with codecs.open(nuspec_file, 'w', 'utf-8') as f:
        f.write(nuspec_content)

    dotnet.nuget_pack(nuspec_file, '-Symbols', '-OutputDirectory', bjoin())


def nuget_push():
    dotnet.nuget_push(bjoin(project_name + '.' + nuget_version() + '.symbols.nupkg'))


def nuget_version():
    ret = project_version
    if prerelease:
        ret += '-b%06d' % build_number
    return ret


def bjoin(*args):
    base_path = os.path.dirname(os.path.realpath(__file__))
    return os.path.join(base_path, *args)
