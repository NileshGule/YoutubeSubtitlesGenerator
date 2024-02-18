# Youtube Subtitles generator

This repo contains the source code for generating the Youtube subtitles in multiple languages. The input file is a .vtt file. This is generated from Youtube video subtitle.
The default language is English. This input file is converted to multiple languages uses Azure Cognitive service named Translator.

## Prerequisites

### Create the Translator service in Azure.
Follow the [documentation](https://docs.microsoft.com/en-ca/azure/cognitive-services/translator/quickstart-translator) to create a new instance of Translator service in Azure.

### Get the API key and store it in the Machine level environment variable
Retrieve the API key for Translator from Azure portal and store it in an environment variable.
I have used Powershell to add the environment variable. You can use any other approach to create an environment variable named `TranslatorAPIkey`.

```Powershell

[System.Environment]::SetEnvironmentVariable('TranslatorAPIkey','YOUR__API__KEY', 'Machine')

```

### List of Languages for subtitles

The subtitles are translated into the languages listed below.
For the complete list of [languages supported](https://docs.microsoft.com/en-ca/azure/cognitive-services/translator/language-support) by the translator service, refer to the documentation.

| Language code | Language name |
| ---           | ---           |
| af            | Afrikaans     |
| sq            | Albanian      |
| ar            | Arabic        |
| hy            | Armenian      |
| bn            | Bangla        |
| bg            | Bulgarian     |
| zh-Hans       | ChineseSimplified |
| hr            | Croatian      |
| cs            | Czek          |
| da            | Danish        |
| nl            | Dutch         |
| fil           | Filipino      |
| fi            | Finnish       |
| fr            | French        |
| de            | German        |
| el            | Greek         |
| he            | Hebrew        |
| hi            | Hindi         |
| hu            | Hungarian     |
| id            | Indonesian    |
| ga            | Irish         |
| it            | Italian       |
| ja            | Japanese      |
| ko            | Korean        |
| ms            | Malay         |
| my            | Myanmar       |
| ne            | Nepali        |
| nb            | Norwegian     |
| fa            | Persian       |
| pl            | Polish        |
| pt-pt         | Portuguese    |
| ro            | Romanian      |
| ru            | Russian       |
| es            | Spanish       |
| sv            | Swedish       |
| th            | Thai          |
| tr            | Turkish       |
| uk            | Ukrainian     |
| vi            | Vietnamese    |

## Create project using YouTube Data API
Follow the instructions in the [YouTube Data API](https://developers.google.com/youtube/v3/getting-started) and create a project in the console

Watch the two YouTUbe videos listed below to know how to generate and upload subtitles in multiple languages.

## References

- [Youtube Data API v3 Client library for .Net](https://developers.google.com/api-client-library/dotnet/apis/youtube/v3)

## YouTube videos

[How to generate Youtube subtitles in multiple languages using Microsoft Translator](https://youtu.be/LJadZFQiHCI)

[![How to generate Youtube subtitles in multiple languages using Microsoft Translator](/images/generate-Youtube-Subtitles.png)](https://youtu.be/LJadZFQiHCI)

[How to programatically upload Youtube subtitles in multiple languages](https://youtu.be/aJPMFAIsApY)

[![How to programatically upload Youtube subtitles in multiple languages](/images/upload-multiple-subtitle.png)](https://youtu.be/aJPMFAIsApY)
