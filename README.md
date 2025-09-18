# warm_lang
The idea is to develop a compiled programming language. The compile target should be the Common Language Runtime (CLR)

## How to run

```console
dotnet run --project WarmLangCompiler <program>
```

You can get command line help by using the `-lh` or `--lang-help` flag(s).

## Running the compiled DLL

The compiler will output a DLL and a `runtimeconfig.json` file both of which must be present to run the program. The DLL compiled program can be run using:

```
dotnet {pathToDll}
```

The runtime configuration file must be named `{nameOfDllWithoutFileExtension}.runtimeconfig.json`.

Here is a the most basic (and currently complete) runtimeconfig that can be copy pasted (NOTE, it uses dotnet 9): 

```json
{
    "runtimeOptions": {
      "tfm": "net9.0",
      "framework": {
        "name": "Microsoft.NETCore.App",
        "version": "9.0.0"
      }
    }
  }
```