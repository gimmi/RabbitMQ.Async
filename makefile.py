import os
import dotnet

base_dir = os.path.dirname(__file__)
project_version = '0.1.0'


def init():
    dotnet.nuget_restore(os.path.join(base_dir, 'src', 'RabbitMQ.Async.sln'))
    dotnet.assembly_info(
        os.path.join(base_dir, 'src', 'SharedAssemblyInfo.cs'),
        AssemblyConfiguration='',
        AssemblyCompany='',
        AssemblyProduct='RabbitMQ.Async',
        AssemblyCopyright='Gian Marco Gherardi',
        AssemblyTrademark='',
        AssemblyVersion=project_version + '.0',
        AssemblyFileVersion=project_version + '.0',
        AssemblyInformationalVersion=project_version
    )

def build():
    dotnet.msbuild()