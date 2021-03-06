`quakemap` is released under GPL version 2 or later.  See the
header comments in `quakemaps.cs` and the file `COPYING`.

When I was first learning C# (for Windows Phone 7 development
while working at Nokia), I developed this program, "quakemap", as an
exercise. It reads map and earthquake data, either from local files or
from a web site, and generates a png image file plotting the locations
of recent quakes. Parameters are specified by command-line options.

Nokia was kind enough to let me retain ownership of the code after
I left.

I did most of the development using Mono on my Ubuntu system at home,
but it also builds and runs under Windows using Microsoft Visual
Studio. My original intent was to port the code to the Windows Phone
platform, but several of the required libraries were unavailable.

This was a learning exercise. I make no claims that this program
is a model of good C# style, or that it's as efficient as it could
be. For example, all the plotting (lat/lon lines, shores, quake data)
is done one pixel at a time.

The generated image uses:

* Color to denote the time of the quake (red is recent, blue is
  3.5 days ago, green is 7 days ago, with continuous shading for
  intermediate ages)
* A black vertical line to denote the depth
* The size of the circle to denote magnitude

I've included `no-rivers.txt` in this project; it's suitable as an
argument to the `-shoredata` option (see below). **Warning**: This file
is about 67 megabytes of plain text, about 3.2 million lines (each
line represents a point on the Earth's surface).

By default, it will read shore data from `shores.txt` and quake
data from `eqs7day-M1.txt`, and write the image to `quakes.png`,
all in the current directory.  If `eqs7day-M1.txt` is not
available, it will read the data from [a file on the USGS web
site](http://earthquake.usgs.gov/earthquakes/catalogs/eqs7day-M1.txt).

The file `quakes-sample.png` is a sample image file generated by the program,
with the following arguments:

    ./quakemap.exe -width 4096 -rotation 117 -shore no-rivers.txt

It was generated at Sun 2012-11-04 22:20 UTC.  The `-rotation 117`
argument centers the map on the longitude of San Diego, 117° West.

The file `quakes-big.png` is a very large (50 megapixels, 2.5 megabytes)
image showing all recorded quakes over a period of about 19 months,
from 2011-03-25 to 2012-11-09.

To compile under Ubuntu, you'll need to install the `mono-gmcs` package:

    sudo apt-get install mono-gmcs

Then run this command:

    gmcs -r:System.Drawing.dll quakemap.cs

This generates `quakemap.exe`, which can be executed.
`quakemap.exe -help` shows the following usage message:

    quakemap running in /home/kst/git/quakemap
    Usage: quakemape.exe [options]
        -help            Show this message and exit
        -width num       Width of generated map, default is 4096
                         Sets height to width/2
        -height num      Height of generated map, default is 2048
                         Sets width to height*2
        -rotation num    Rotate num degrees
        -mercator        Use a Mercator projection
        -fade            Older earthquakes fade to white
        -gray            Show quakes in gray (overrides -fade)
        -quakedata name  Filename or URL of quake data file
                         May be repeated; first available name is used
                         Default list is:
            eqs7day-M1.txt
            http://earthquake.usgs.gov/earthquakes/catalogs/eqs7day-M1.txt
        -shoredata name  Filename or URL of shore data file
                         Default list is:
            shores.txt
        -imagefile name  Name of generated image file, should be *.png
                         Default list is:
            quakes.png

(Some of the defaults are specific to the locations of files on my
own current or former systems.)

-- Keith Thompson <Keith.S.Thompson@gmail.com> Mon 2012-11-12
