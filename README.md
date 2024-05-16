Basically the same software, but with SignalR and BassFX support that enable you play sounds with tweaked pitch and tempo effects.

# To do
1. Linux, MacOS support for BassFX		
2. SignalR authentication and custom port
3. Log who played what sound

# How to build
1. Modify RuntimeIdentifier in `AmplitudeSoundboard.csproj`,
to select the target platform: `win-x64`, `linux-x64`, `osx-x64`
```XML
<PropertyGroup>
  <!--<RuntimeIdentifier>osx-x64</RuntimeIdentifier>-->
  <!--<RuntimeIdentifier>win-x64</RuntimeIdentifier>-->
  <RuntimeIdentifier>linux-x64</RuntimeIdentifier>
</PropertyGroup>
<PropertyGroup>
  <DefineConstants Condition="'$(RuntimeIdentifier)' == 'win-x64'">$(DefineConstants);Windows</DefineConstants>
  <DefineConstants Condition="'$(RuntimeIdentifier)' == 'osx-x64'">$(DefineConstants);MacOS</DefineConstants>
  <DefineConstants Condition="'$(RuntimeIdentifier)' == 'linux-x64'">$(DefineConstants);Linux</DefineConstants>
</PropertyGroup>
```
2. Build