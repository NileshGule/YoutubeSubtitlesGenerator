# Youtube Subtitles generator

This repo contains the source code for generating the Youtube subtitles in multiple languages. The input file is a .vtt file. This is generated from Youtube video subtitle.
The default language is English. This input file is converted to multiple languages uses Azure Congnitive service named Translator.

## Prerequisites

### Create the Translator service in Azure.
Follow the [documentation](https://docs.microsoft.com/en-ca/azure/cognitive-services/translator/quickstart-translator) to create a new instance of Translator service in Azure.

### Get the API key and store it in the Machine level environment variable
Retrieve the API key for Translator from Azure portal and store it in an environment variable.
I have used Powershell to add the environment variable. You can use any other approach to create an environment variable named `TranslatorAPIkey`.

```Powershell```

[System.Environment]::SetEnvironmentVariable('TranslatorAPIkey','YOUR__API__KEY', 'Machine')

```
