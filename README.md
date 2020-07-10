# static-pages-downloader
All static pages under the site URL will be downloaded.

# How to use

1. Save the file from [Release](https://github.com/Takmg/static-pages-downloader/releases) and decompress it.
  
2. Rewrite settings.yml
   
    * download_uri ... Specify the URL to download.
    * ignore_list ... Specify a URL that is not saved locally.
    * save_path   ... Save destination directory name.
    * search_depth ... Specify the depth to search.
    * encoding ... Specify the Html Encoding.
    * multithread ... Run with multi-thread.(true or false)
    * basetag_relative ... Replace the resource URI and link URI with the relative path from the base tag.(true or false) \
       If false, it will be replaced with the relative path from the HTML file.

3. Execute the 'run.bat' or '<span>run.sh</span>'

    * Windows version launches 'run.bat'
    * Linux version or Mac version launches '<span>run.sh</span>'
    * This application is required .net core 3.1 for [Windows version](https://docs.microsoft.com/dotnet/core/install/windows?tabs=netcore31) or [Linux version](https://docs.microsoft.com/dotnet/core/install/linux) or [Mac version](https://docs.microsoft.com/dotnet/core/install/macos).
    * It can be very time consuming on some sites

# License

GNU LESSER GENERAL PUBLIC LICENSE\
Version 2.1, February 1999

Copyright (C) 2020 Takmg

[License](https://github.com/Takmg/static-pages-downloader/blob/master/LICENSE) for this program


# Used library licenses

**cs-script**

Project URL ... [https://github.com/oleg-shilo/cs-script](https://github.com/oleg-shilo/cs-script)

The MIT License (MIT)

Copyright (c) 2018 oleg-shilo

[License View All](https://github.com/oleg-shilo/cs-script/blob/master/LICENSE)

------

**HtmlAgilityPack**

Project URL ... [https://html-agility-pack.net/](https://html-agility-pack.net/)

The MIT License (MIT)

[License View All](https://github.com/zzzprojects/html-agility-pack/blob/master/LICENSE)

----- 

**YamlDotNet**

Project URL ... [https://github.com/aaubry/YamlDotNet/](https://github.com/aaubry/YamlDotNet/)

The MIT License (MIT)

Copyright (c) 2008, 2009, 2010, 2011, 2012, 2013, 2014 Antoine Aubry and contributors

[License View All](https://github.com/aaubry/YamlDotNet/blob/master/LICENSE.txt)

----- 

**jsbeautifylib**

Project URL ... [https://archive.codeplex.com/?p=jsbeautifylib](https://archive.codeplex.com/?p=jsbeautifylib)


-----
