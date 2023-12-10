[![NuGet](https://img.shields.io/nuget/vpre/Shard.WebsiteScraper)](https://www.nuget.org/packages/Shard.WebsiteScraper) [![Downloads](https://img.shields.io/nuget/dt/Shard.WebsiteScraper)](https://www.nuget.org/packages/Shard.WebsiteScraper) [![License](https://img.shields.io/github/license/typnull/WebsiteScraper.svg)](https://github.com/TypNull/WebsiteScraper/blob/master/LICENSE.txt)
# WebsiteScraper

WebsiteScraper is a powerful library that allows you to easily download comics and manga from various websites. With its intuitive interface and advanced parsing capabilities, you can quickly fetch and save your favorite content for offline reading. This readme provides an overview of how to use the WebsiteScraper library and includes some example code snippets to get you started.

## Installation

To use WebsiteScraper in your project, you can install it via NuGet package manager or by manually adding the library to your project references.

## Usage

1. Import the WebsiteScraper library in your code:

```csharp
using WebsiteScraper;
```

2. Create a Website object:

```csharp
Website website = Website.LoadWebsite("Destination");
```

Make sure to replace "Destination" with the destination file of the website you want to scrape. An example for a website file is provided in the repository.

3. Download all links for new and recommended comics:

```csharp
Comic[] newComics = website.LoadNewsAsync<Comic>();
Comic[] recommendedComics = website.LoadExtraAsync<Comic>("Recommended");
```

These methods fetch all the links for new and recommended comics respectively. The `Comic` class should be defined based on your specific website structure.

4. Get comic information:

```csharp
comic.UpdateAsync();
```

This method retrieves and updates the comic information for the previously loaded links.

5. Download the first chapter of a comic:

```csharp
comic.Chapter[0].DownloadAsync("Destination");
```
Replace "Destination" with the desired location to save the downloaded chapter.

## Example

An example for a website is provided in the repository. You can refer to this example for a better understanding of how to use the WebsiteScraper library in your own projects.

Please note that the example provided may need to be modified based on the structure and requirements of the specific website you are targeting.

Easily add your own websites with the Website Creator WPF application.

## Contributing

Contributions to the WebsiteScraper library are welcome. If you encounter any bugs, have feature requests, or want to improve the library in any way, please feel free to open an issue or submit a pull request.

## License

The WebsiteScraper library is released under the [MIT License](https://opensource.org/licenses/MIT). You are free to use, modify, and distribute the library in any way you see fit.


## **Free Code** and **Free to Use**
#### Have fun!
