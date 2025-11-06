# warm_lang
The idea is to develop a compiled programming language. The compile target should be the Common Language Runtime (CLR)

## Credit

This project builds upon ideas and code from Immo Landwerth's [minsk](https://github.com/terrajobst/minsk) compiler project, so please do consider checking out the interesting [YouTube series](https://www.youtube.com/playlist?list=PLRAdsfhKI4OWNOSfS7EUu5GRAVmze1t2y) containg with vods of its development. My own contributions include: primitive list type, generics, type inference on these generics and list type, higher-order functions, (some bugs), and hopefully more features in the future.

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