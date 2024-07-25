# warm_lang
The idea is to develop a compiled programming language. The compile target should be the Common Language Runtime (CLR)

## How to run

Use the following command (or your favourite IDE play button)
```console
dotnet run --project WarmLangCompiler
```

Or you can provide a target by arguement:

```console
dotnet run --project WarmLangCompiler <program>
```

You can get command line help by using the `-lh` or `--lang-help` flag(s).

## Running the compiled DLL

The compiler will output a DLL. The DLL must be run using dotnet

```
dotnet {pathToDll}
```

and will require a `runtimeconfig.json` in the same directory. 
The file needs be named as `{nameOfDllWithoutFileExtension}.runtimeconfig.json`. Meaning for a compiled DLL called `out.dll`, you will need a config file called `out.runtimeconfig.json`.

Here is a the most basic (and currently complete) runtimeconfig that can be copy pasted (NOTE, it requires dotnet 7.0): 

```json
{
    "runtimeOptions": {
      "tfm": "net7.0",
      "framework": {
        "name": "Microsoft.NETCore.App",
        "version": "7.0.0"
      }
    }
  }
```