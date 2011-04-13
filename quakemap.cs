// $Id: quakemap.cs,v 1.23 2011/04/13 23:58:52 kst Exp $
// $Source: /home/kst/CVS_smov/csharp/quakemap.cs,v $

using System;
using System.IO;
using System.Net;
using System.Text;
using System.Collections;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Drawing;
using System.Drawing.Imaging;

namespace quakemap
{
    struct Constants
    {
        public static readonly int defaultWidth = 4096;
        public static readonly CultureInfo enUS = new CultureInfo("en-US");
        public static readonly DateTime now = DateTime.UtcNow;
        public static readonly string dateFormat = @"dddd, MMMM d, yyyy HH:mm:ss \U\T\C";
        public static readonly Color bgColor    = Color.White;
        public static readonly Color axisColor  = Color.Gray;
        public static readonly Color shoreColor = Color.Cyan;
        public static readonly int   bgARGB     = bgColor.ToArgb();
        public static readonly int   axisARGB   = axisColor.ToArgb();
        public static readonly int   shoreARGB  = shoreColor.ToArgb();
    }

    struct Options
    {
        public static int width = Constants.defaultWidth;
        public static int height { get { return width / 2; } }
        public static bool mercator = false;
        public static string[] quakeData =
            {
                "/home/kst/cvs-smov/downloads/all-quakes.out",
                "http://earthquake.usgs.gov/earthquakes/catalogs/eqs7day-M1.txt"
            };
        public static string[] shoreData =
            {
                "/home/kst/public_html/shores.txt",
                "http://smov.org/~kst/shores.txt"
            };
        public static string[] imageFile =
            {
                "/home/kst/public_html/quakes.png",
                @"H:\public_html\quakes.png"
            };
    }

    struct Point
    {
        int m_x;
        int m_y;

        public int x { get { return m_x; } }
        public int y { get { return m_y; } }

        public Point(double lat, double lon)
        {
            // Scale lon from (-180..+180) to (0..width-1)
            // Scale lat from ( -90.. +90) to (0..height-1)

            if (! Options.mercator)
            {
                double y = lat / 90.0;
                lon *= Math.Sqrt(1.0 - y*y);
            }

            m_x = (int)((lon + 180.0) / 360.0 * Options.width);
            m_y = (int)((lat +  90.0) / 180.0 * Options.height);
            // North at the top
            m_y = Options.height - m_y;
        }

        public Point(Quake q)
        {
            this = new Point(q.Lat, q.Lon);
        }
    }

    struct Quake
    {
        public string Src;
        public string Eqid;
        public string Version;
        public string Datetime;
        public double Lat;
        public double Lon;
        public double Magnitude;
        public double Depth;
        public int    NST;
        public string Region;

        public DateTime dt;
        public TimeSpan age;

        public override string ToString()
        {
            return "Src=" + Src + ", Eqid=" + Eqid + ", Version=" + Version + 
                   ", Datetime=" + Datetime + ", Lat=" + Lat + ", Lon=" + Lon +
                   ", Magnitude=" + Magnitude + ", Depth=" + Depth +
                   ", NST=" + NST + ", Region=" + Region +
                   ", dt=" + dt.ToString("u", Constants.enUS) +
                   ", age=" + age;
        }
        
        public Color color
        {
            get
            {
                // Age 0               : red
                // Age 3.5 days        : blue
                // Age 7 days or older : green
                // Intermediate values are interpolated
                int red, green, blue;
                double ageInDays = age.TotalDays;
                if (ageInDays > 7.0) ageInDays = 7.0;
                if (ageInDays < 0.0) ageInDays = 0.0;
                if (ageInDays <= 3.5)
                {
                    double b = ageInDays / 3.5; // 0..1
                    blue = (int)(b * 256);
                    if (blue > 255) blue = 255;
                    red = 255 - blue;
                    green = 0;
                }
                else
                {
                    double g = (ageInDays - 3.5) / 3.5; // 0..1
                    green = (int)(g * 256);
                    if (green > 255) green = 255;
                    blue = 255 - green;
                    red = 0;
                }
                return Color.FromArgb(red, green, blue);
            }
        }

        public void Plot(Bitmap bitmap)
        {
            Point p = new Point(this);
            // Console.Write("p.x = " + p.x + ", p.y = " + p.y + ", mag = " + (int)Magnitude);
            Color color = this.color;
            int side = (int)(Magnitude * 2);
            int xmin = Math.Max(p.x - side / 2, 0);
            int xmax = Math.Min(p.x + side / 2, Options.width - 1);
            int ymin = Math.Max(p.y - side / 2, 0);
            int ymax = Math.Min(p.y + side / 2, Options.height - 1);
            for (int x = xmin; x <= xmax; x ++)
            {
                for (int y = ymin; y <= ymax; y ++)
                {
                    bitmap.SetPixel(x, y, color);
                }
            }
        }

    }

    public class Program
    {
        static public StreamReader OpenStream(string s)
        {
            if (s.Contains("://"))
            {
                try
                {
                    WebRequest request = WebRequest.Create(s);
                    WebResponse response = request.GetResponse();
                    return new StreamReader(response.GetResponseStream(), Encoding.ASCII);
                }
                catch
                {
                    return null;
                }
            }
            else
            {
                try
                {
                    return new StreamReader(s);
                }
                catch
                {
                    return null;
                }
            }
        }

        static public StreamReader OpenStream(string[] list)
        {
            foreach (string s in list)
            {
                StreamReader reader = OpenStream(s);
                if (reader != null) return reader;
            }
            if (list.Length > 1)
            {
                Console.Error.WriteLine("Cannot open any of:");
                foreach (string s in list)
                {
                    Console.Error.WriteLine("    \"" + s + "\"");
                }
            }
            else
            {
                Console.Error.WriteLine("Cannot open \"" + list[0] + "\"");
            }
            Environment.Exit(1);
            return null;
        }

        static public void SaveBitmapToPng(Bitmap bitmap, string[] list)
        {
            foreach (string filename in list)
            {
                try
                {
                    bitmap.Save(filename, ImageFormat.Png);
                    Console.WriteLine("Saved to " + filename);
                    return;
                }
                catch
                {
                }
            }
            if (list.Length > 1)
            {
                Console.Error.WriteLine("Cannot create any of:");
                foreach (string s in list)
                {
                    Console.Error.WriteLine("    \"" + s + "\"");
                }
            }
            else
            {
                Console.Error.WriteLine("Cannot open \"" + list[0] + "\"");
            }
            Environment.Exit(1);
        }

        static public void SaveBitmapToPng(Bitmap bitmap, string filename )
        {
            bitmap.Save(filename, ImageFormat.Png);
            Console.WriteLine("Saved to " + filename);
        }

        static public void Help(string error = null)
        {
            if (error != null) Console.WriteLine(error);
            Console.WriteLine("Usage: quakemape.exe [options]");
            Console.WriteLine("    -help            Show this message and exit");
            Console.WriteLine("    -width num       Width of generated map, default is 1024");
            Console.WriteLine("    -mercator        Use a Mercator projection");
            Console.WriteLine("    -quakedata name  Filename or URL of quake data file");
            Console.WriteLine("    -shoredata name  Filename or URL of shore data file");
            Console.WriteLine("    -imagefile name  Name of generated image file, should be *.png");
            Environment.Exit(1);
        }

        enum argFlag { none, width, quakeData, shoreData, imageFile };

        static bool Matches(string arg, string name, int minLen)
        {
            return arg.Length >= minLen && name.StartsWith(arg);
        }

        static public void Main(string[] args)
        {
            Console.WriteLine("quakemap");

            argFlag flag = argFlag.none;
            foreach (string arg in args)
            {
                switch (flag)
                {
                    case argFlag.none:
                        if (Matches(arg, "-help", 2))
                        {
                            Help();
                        }
                        else if (Matches(arg, "-width", 2))
                        {
                            flag = argFlag.width;
                        }
                        else if (Matches(arg, "-quakedata", 2))
                        {
                            flag = argFlag.quakeData;
                        }
                        else if (Matches(arg, "-shoredata", 2))
                        {
                            flag = argFlag.shoreData;
                        }
                        else if (Matches(arg, "-imagefile", 2))
                        {
                            flag = argFlag.imageFile;
                        }
                        else
                        {
                            Help("Unrecognized argument \"" + arg + "\"");
                        }
                        break;
                    case argFlag.width:
                        try
                        {
                            Options.width = Convert.ToInt32(arg);
                        }
                        catch
                        {
                            Help("Invalid width argument: \"" + arg + "\"");
                        }
                        flag = argFlag.none;
                        break;
                    case argFlag.quakeData:
                        Options.quakeData = new string[] { arg };
                        flag = argFlag.none;
                        break;
                    case argFlag.shoreData:
                        Options.shoreData = new string[] { arg };
                        flag = argFlag.none;
                        break;
                    case argFlag.imageFile:
                        Options.imageFile = new string[] { arg };
                        flag = argFlag.none;
                        break;
                }
            }
            if (flag != argFlag.none)
            {
                Help("Missing argument");
            }

            Console.WriteLine("Options:");
            Console.WriteLine("    width:    " + Options.width);
            Console.WriteLine("    height:   " + Options.height);
            Console.WriteLine("    mercator: " + Options.mercator);
            Console.WriteLine("    quakeData:");
            foreach (string s in Options.quakeData)
            {
                Console.WriteLine("        \"" + s + "\"");
            }
            Console.WriteLine("    shoreData:");
            foreach (string s in Options.shoreData)
            {
                Console.WriteLine("        \"" + s + "\"");
            }
            Console.WriteLine("    imageFile:");
            foreach (string s in Options.imageFile)
            {
                Console.WriteLine("        \"" + s + "\"");
            }

            using (StreamReader reader = OpenStream(Options.quakeData))
            {
                string line1 = reader.ReadLine();
                string[] headers = line1.Split(new char[] {','}, StringSplitOptions.None);
                for (int i = 0; i < headers.Length; i ++)
                {
                    Console.Write(headers[i]);
                    if (i < headers.Length - 1)
                    {
                        Console.Write('|');
                    }
                }
                Console.WriteLine();

                string pattern = "^";
                for (int i = 0; i <= 9; i ++)
                {
                    if (i == 3 || i == 9)
                    {
                        pattern += "\"([^\"]*)\"";
                    }
                    else
                    {
                        pattern += "([^,]*)";
                    }
                    if (i < 9)
                    {
                        pattern += ",";
                    }
                }
                pattern += "$";
                Console.WriteLine("pattern = \"" + pattern + "\"");
                Regex line_re = new Regex(pattern);

                string line;
                int lineCount = 0;
                ArrayList quakes = new ArrayList();
                while ((line = reader.ReadLine()) != null)
                {
                    lineCount ++;
                    Match m = line_re.Match(line);
                    if (m.Success)
                    {
                        Quake q = new Quake();
                        q.Src       = m.Groups[1].Value;
                        q.Eqid      = m.Groups[2].Value;
                        q.Version   = m.Groups[3].Value;
                        q.Datetime  = m.Groups[4].Value;
                        q.Lat       = Convert.ToDouble(m.Groups[5].Value);
                        q.Lon       = Convert.ToDouble(m.Groups[6].Value);
                        q.Magnitude = Convert.ToDouble(m.Groups[7].Value);
                        q.Depth     = Convert.ToDouble(m.Groups[8].Value);
                        q.NST       = Convert.ToInt32(m.Groups[9].Value);
                        q.Region    = m.Groups[10].Value;
                        DateTimeStyles style = DateTimeStyles.AllowWhiteSpaces |
                                               DateTimeStyles.AdjustToUniversal |
                                               DateTimeStyles.AssumeUniversal;
                        bool ok = DateTime.TryParseExact(q.Datetime, Constants.dateFormat, Constants.enUS, style, out q.dt);
                        if (!ok)
                        {
                            q.dt = DateTime.MinValue;
                        }
                        q.age = Constants.now.Subtract(q.dt);
                        quakes.Add(q);
                    }
                    else
                    {
                        Console.WriteLine("Line \"" + line + "\" does not match Regex " + line_re);
                        Environment.Exit(1);
                    }
                }

                Console.WriteLine("Got " + lineCount + " lines, " + quakes.Count + " earthquakes");

                double minLat = Double.MaxValue;
                double maxLat = Double.MinValue;
                double minLon = Double.MaxValue;
                double maxLon = Double.MinValue;
                double minMagnitude = Double.MaxValue;
                double maxMagnitude = Double.MinValue;
                TimeSpan minAge = TimeSpan.MaxValue;
                TimeSpan maxAge = TimeSpan.MinValue;
                quakes.Reverse(); // plot older quakes first
                foreach (Quake q in quakes)
                {
                    if (q.Lat < minLat) minLat = q.Lat;
                    if (q.Lat > maxLat) maxLat = q.Lat;
                    if (q.Lon < minLon) minLon = q.Lon;
                    if (q.Lon > maxLon) maxLon = q.Lon;
                    if (q.Magnitude < minMagnitude) minMagnitude = q.Magnitude;
                    if (q.Magnitude > maxMagnitude) maxMagnitude = q.Magnitude;
                    if (q.age < minAge) minAge = q.age;
                    if (q.age > maxAge) maxAge = q.age;
                }
                Console.WriteLine("Lat: " + minLat + " .. " + maxLat);
                Console.WriteLine("Lon: " + minLon + " .. " + maxLon);
                Console.WriteLine("Magnitude: " + minMagnitude + " .. " + maxMagnitude);
                Console.WriteLine("Age: " + minAge + " .. " + maxAge);

                Bitmap bitmap;
                try
                {
                    bitmap = new Bitmap(Options.width, Options.height);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception creating bitmap: " + e);
                    bitmap = null;
                    Environment.Exit(1);
                }

                Console.WriteLine("Initializing blank screen");
                for (int y = 0; y < Options.height; y ++)
                {
                    for (int x = 0; x < Options.width; x ++)
                    {
                        bitmap.SetPixel(x, y, Constants.bgColor);
                    }
                }

                Console.WriteLine("Setting shores");
                StreamReader shores = OpenStream(Options.shoreData);
                string shore;
                int shorePoints = 0;
                int shorePixels = 0;
                while ((shore = shores.ReadLine()) != null)
                {
                    string[] words = shore.Split(new Char[] {' '});
                    double lat = Convert.ToDouble(words[0]);
                    double lon = Convert.ToDouble(words[1]);
                    if (lon < -180) lon += 360;
                    if (lon > +180) lon -= 360;
                    Point p = new Point(lat, lon);
                    shorePoints ++;
                    if (bitmap.GetPixel(p.x, p.y).ToArgb() != Constants.shoreARGB)
                    {
                        bitmap.SetPixel(p.x, p.y, Constants.shoreColor);
                        shorePixels ++;
                    }
                }
                double percentage = (double)shorePixels / (double)shorePoints * 100.0;
                Console.WriteLine("Plotted " + shorePixels + " pixels for " + shorePoints + " points" +
                                  " (" + percentage.ToString("0.##") + "%)");

                Console.WriteLine("Setting axes");
                for (int y = 0; y < Options.height; y ++)
                {
                    bitmap.SetPixel(0,               y, Constants.axisColor);
                    bitmap.SetPixel(Options.width/2, y, Constants.axisColor);
                    bitmap.SetPixel(Options.width-1, y, Constants.axisColor);
                }
                for (int x = 0; x < Options.width; x ++)
                {
                    bitmap.SetPixel(x, 0,                Constants.axisColor);
                    bitmap.SetPixel(x, Options.height/2, Constants.axisColor);
                    bitmap.SetPixel(x, Options.height-1, Constants.axisColor);
                }

                Console.WriteLine("Iterating over quakes");
                foreach (Quake q in quakes)
                {
                    q.Plot(bitmap);
                }
                SaveBitmapToPng(bitmap, Options.imageFile);
            }
        }
    }
}
