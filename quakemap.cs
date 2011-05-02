// $Id: quakemap.cs,v 1.36 2011/05/02 00:49:55 kst Exp $
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

namespace Quakemap
{
    struct Constants
    {
        public static readonly CultureInfo enUS = new CultureInfo("en-US");
        public static readonly DateTime now = DateTime.UtcNow;
        public static readonly string dateFormat = @"dddd, MMMM d, yyyy HH:mm:ss \U\T\C";
        public static readonly DateTimeStyles dateStyle =
            DateTimeStyles.AllowWhiteSpaces |
            DateTimeStyles.AdjustToUniversal |
            DateTimeStyles.AssumeUniversal;
        public static readonly Color bgColor    = Color.White;
        public static readonly Color axisColor  = Color.Gray;
        public static readonly Color shoreColor = Color.Gray;
        public static readonly Color depthColor = Color.Black;
        public static readonly int   bgARGB     = bgColor.ToArgb();
        public static readonly int   axisARGB   = axisColor.ToArgb();
        public static readonly int   shoreARGB  = shoreColor.ToArgb();
    } // struct Constants

    public class Options
    {
        private struct Default
        {
            public static readonly int Height = 2048;
            public static readonly bool Mercator = false;
            public static readonly ArrayList QuakeData = new ArrayList(new string[]
                {
                    "/home/kst/cvs-smov/downloads/eqs7day-M1.txt",
                    "http://earthquake.usgs.gov/earthquakes/catalogs/eqs7day-M1.txt"
                });
            public static readonly ArrayList ShoreData = new ArrayList(new string[]
                {
                    "/home/kst/public_html/shores.txt",
                    @"C:\cygwin\home\keithomp\git\local\quakemap\shores.txt",
                    "http://smov.org/~kst/shores.txt"
                });
            public static readonly ArrayList ImageFile = new ArrayList(new string[]
                {
                    "/home/kst/public_html/quakes.png",
                    @"H:\public_html\quakes.png"
                });
        }

        private int? m_height = null;
        public int height
        {
            get { return m_height == null ? Default.Height : (int)m_height; }
            set { m_height = value; }
        }
        public int width {
            get { return 2 * height; }
            set { m_height = value / 2; }
        }
        public bool? m_mercator = null;
        public bool mercator
        {
            get { return m_mercator == null ? Default.Mercator : (bool)m_mercator; }
            set { m_mercator = value; }
        }

        private ArrayList m_quakeData = null;
        public ArrayList quakeData
        {
            get { return m_quakeData == null ? Default.QuakeData : m_quakeData; }
        }
        public void AddQuakeData(string s)
        {
            if (m_quakeData == null)
            {
                m_quakeData = new ArrayList();
            }
            m_quakeData.Add(s);
        }

        private ArrayList m_shoreData = null;
        public ArrayList shoreData
        {
            get { return m_shoreData == null ? Default.ShoreData : m_shoreData; }
        }
        private void AddShoreData(string s)
        {
            if (m_shoreData == null)
            {
                m_shoreData = new ArrayList();
            }
            m_shoreData.Add(s);
        }

        private ArrayList m_imageFile = null;
        public ArrayList imageFile
        {
            get { return m_imageFile == null ? Default.ImageFile : m_imageFile; }
        }
        public void AddImageFile(string s)
        {
            if (m_imageFile == null)
            {
                m_imageFile = new ArrayList();
            }
            m_imageFile.Add(s);
        }

        enum argFlag { none, width, height, quakeData, shoreData, imageFile };

        public Options(string[] args)
        {
            argFlag flag = argFlag.none;
            foreach (string arg in args)
            {
                switch (flag)
                {
                    case argFlag.none:
                        if (Matches(arg, "-help", 4))
                        {
                            Usage();
                        }
                        else if (Matches(arg, "-mercator", 2))
                        {
                            mercator = true;
                        }
                        else if (Matches(arg, "-width", 2))
                        {
                            flag = argFlag.width;
                        }
                        else if (Matches(arg, "-height", 4))
                        {
                            flag = argFlag.height;
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
                            Usage("Unrecognized argument \"" + arg + "\"");
                        }
                        break;
                    case argFlag.width:
                        try
                        {
                            width = Convert.ToInt32(arg);
                        }
                        catch
                        {
                            Usage("Invalid width argument: \"" + arg + "\"");
                        }
                        flag = argFlag.none;
                        break;
                    case argFlag.height:
                        try
                        {
                            height = Convert.ToInt32(arg);
                        }
                        catch
                        {
                            Usage("Invalid height argument: \"" + arg + "\"");
                        }
                        flag = argFlag.none;
                        break;
                    case argFlag.quakeData:
                        AddQuakeData(arg);
                        flag = argFlag.none;
                        break;
                    case argFlag.shoreData:
                        AddShoreData(arg);
                        flag = argFlag.none;
                        break;
                    case argFlag.imageFile:
                        AddImageFile(arg);
                        flag = argFlag.none;
                        break;
                }
            }
            if (flag != argFlag.none)
            {
                Usage("Missing argument");
            }
            
        } // Options constructor

        static public void Usage(string error = null)
        {
            if (error != null) Console.WriteLine(error);
            Console.WriteLine("Usage: quakemape.exe [options]");
            Console.WriteLine("    -help            Show this message and exit");
            Console.WriteLine("    -width num       Width of generated map, default is " + Default.Height);
            Console.WriteLine("                     Sets height to width/2");
            Console.WriteLine("    -height num      Height of generated map, default is " + Default.Height * 2);
            Console.WriteLine("                     Sets width to height*2");
            Console.WriteLine("    -mercator        Use a Mercator projection");
            Console.WriteLine("    -quakedata name  Filename or URL of quake data file");
            Console.WriteLine("                     May be repeated; first available name is used");
            Console.WriteLine("                     Default list is:");
            foreach (string s in Default.QuakeData)
            {
                Console.WriteLine("        " + s);
            }
            Console.WriteLine("    -shoredata name  Filename or URL of shore data file");
            Console.WriteLine("                     Default list is:");
            foreach (string s in Default.ShoreData)
            {
                Console.WriteLine("        " + s);
            }
            Console.WriteLine("    -imagefile name  Name of generated image file, should be *.png");
            Console.WriteLine("                     Default list is:");
            foreach (string s in Default.ImageFile)
            {
                Console.WriteLine("        " + s);
            }
            Environment.Exit(1);
        } // Usage


        static bool Matches(string arg, string name, int minLen)
        {
            return arg.Length >= minLen && name.StartsWith(arg);
        } // Matches
    } // class Options

    struct Position
    {
        double m_lon;
        double m_lat;
        public double lon { get { return m_lon; } }
        public double lat { get { return m_lat; } }

        public Position(double lon, double lat)
        {
            m_lon = lon;
            m_lat = lat; 
        }

        private static Position nullPosition
        {
            get { return new Position(Double.MaxValue, Double.MaxValue); }
        }

        public bool isNull
        {
            get { return m_lon == Double.MaxValue && m_lat == Double.MaxValue; }
        }

        public Position NextPixelSouth()
        {
            Position result = this; // ok to copy, it's a struct
            double delta = 180.0 / Program.options.height;
            result.m_lon = m_lon;
            result.m_lat = m_lat - delta;
            if (result.m_lat >= -90.0)
            {
                return result;
            }
            else
            {
                return nullPosition;
            }
        }

        public Position NextPixelEast()
        {
            Position result = this; // ok to copy, it's a struct
            double delta = 360.0 / Program.options.width;
            result.m_lon = m_lon + delta;
            // NOTE: This plots more pixels than it needs to in the
            // non-Mercator projection, but that's ok for now.
            result.m_lat = m_lat;
            if (result.m_lon <= 180.0)
            {
                return result;
            }
            else
            {
                return nullPosition;
            }
        }

        public override string ToString()
        {
            return "Position(lon=" + m_lon + ",lat=" + m_lat + ")";
        }
    }

    struct Point
    {
        int m_x;
        int m_y;

        public int x { get { return m_x; } }
        public int y { get { return m_y; } }

        public Point(Position pos)
        {
            // Scale lon from (-180..+180) to (0..width-1)
            // Scale lat from ( -90.. +90) to (0..height-1)
            double lon = pos.lon;
            double lat = pos.lat;

            if (! Program.options.mercator)
            {
                double y1 = lat / 90.0;
                lon *= Math.Sqrt(1.0 - y1*y1);
            }

            m_x = (int)((lon + 180.0) / 360.0 * Program.options.width);
            m_y = (int)((lat +  90.0) / 180.0 * Program.options.height);
            // North at the top
            m_y = Program.options.height - m_y;
        }

        public Point(Quake q)
        {
            this = new Point(q.position);
        }

        public override string ToString()
        {
            return "Point((x=" + m_x + ",y=" + m_y + ")";
        }
    }

    class Quake : IComparable
    {
        public string src;
        public string eqid;
        public string version;
        public string datetime;
        public Position position;
        public double magnitude;
        public double depth;
        public int    nst;
        public string region;

        public DateTime dt;
        public TimeSpan age;

        public override string ToString()
        {
            return "src=" + src + ", eqid=" + eqid + ", version=" + version + 
                   ", datetime=" + datetime + ", position=" + position +
                   ", magnitude=" + magnitude + ", depth=" + depth +
                   ", nst=" + nst + ", region=" + region +
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
            // Console.Write("p.x = " + p.x + ", p.y = " + p.y + ", mag = " + (int)magnitude);
            Color color = this.color;
            double radius = magnitude * Program.options.width / 360.0 / 5.0; // ~ 0.2 deg / magnitude
            int iRadius = (int)radius;
            int rSquared = (int)(radius*radius);
            // {x,y}{min,max} are relative to p.{x,y}
            int xmin = Math.Max(-iRadius, -p.x);
            int xmax = Math.Min(+iRadius, Program.options.width-p.x-1);
            int ymin = Math.Max(-iRadius, -p.y);
            int ymax = Math.Min(+iRadius, Program.options.height-p.y-1);
            for (int x = xmin; x <= xmax; x ++)
            {
                for (int y = ymin; y <= ymax; y ++)
                {
                    if (x*x + y*y <= rSquared)
                    {
                        bitmap.SetPixel(p.x+x, p.y+y, color);
                    }
                }
            }
            // Plot the depth as a vertical line
            // Try 1km = 0.05 deg
            int depthY = (int)(p.y + depth * Program.options.width / 360.0 * 0.05);
            if (depthY < 0) depthY = 0;
            for (int y = p.y; y <= depthY; y ++)
            {
                if (p.x < 0 || p.x >= Program.options.width ||
                    y < 0 || y > Program.options.height)
                {
                    Console.WriteLine("depth = " + depth + ", p.y = " + p.y + ", depthY = " + depthY);
                }
                bitmap.SetPixel(p.x, y, Constants.depthColor);
            }
        }

        public int CompareTo(object obj)
        {
            Quake other = obj as Quake;
            return - this.age.CompareTo(other.age);
        }

    } // class Quake


    public class Program
    {
        public static Options options;

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

        static public StreamReader OpenStream(ArrayList list)
        {
            foreach (string s in list)
            {
                StreamReader reader = OpenStream(s);
                if (reader != null) return reader;
            }
            if (list.Count > 1)
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

        static public void SaveBitmapToPng(Bitmap bitmap, ArrayList list)
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
            if (list.Count > 1)
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

        static public void Main(string[] args)
        {
            Console.WriteLine("quakemap running in " + Environment.CurrentDirectory);

            options = new Options(args);

            Console.WriteLine("Options:");
            Console.WriteLine("    width:    " + options.width);
            Console.WriteLine("    height:   " + options.height);
            Console.WriteLine("    mercator: " + options.mercator);
            Console.WriteLine("    quakeData:");
            foreach (string s in options.quakeData)
            {
                Console.WriteLine("        \"" + s + "\"");
            }
            Console.WriteLine("    shoreData:");
            foreach (string s in options.shoreData)
            {
                Console.WriteLine("        \"" + s + "\"");
            }
            Console.WriteLine("    imageFile:");
            foreach (string s in options.imageFile)
            {
                Console.WriteLine("        \"" + s + "\"");
            }

            using (StreamReader reader = OpenStream(options.quakeData))
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
                        q.src       = m.Groups[1].Value;
                        q.eqid      = m.Groups[2].Value;
                        q.version   = m.Groups[3].Value;
                        q.datetime  = m.Groups[4].Value;
                        double lat = Convert.ToDouble(m.Groups[5].Value);
                        double lon = Convert.ToDouble(m.Groups[6].Value);
                        q.position  = new Position(lon, lat);
                        q.magnitude = Convert.ToDouble(m.Groups[7].Value);
                        q.depth     = Convert.ToDouble(m.Groups[8].Value);
                        q.nst       = Convert.ToInt32(m.Groups[9].Value);
                        q.region    = m.Groups[10].Value;
                        bool ok = DateTime.TryParseExact
                                      ( q.datetime,
                                        Constants.dateFormat,
                                        Constants.enUS,
                                        Constants.dateStyle,
                                        out q.dt );
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
                quakes.Sort(); // plot older quakes first
                foreach (Quake q in quakes)
                {
                    if (q.position.lat < minLat) minLat = q.position.lat;
                    if (q.position.lat > maxLat) maxLat = q.position.lat;
                    if (q.position.lon < minLon) minLon = q.position.lon;
                    if (q.position.lon > maxLon) maxLon = q.position.lon;
                    if (q.magnitude < minMagnitude) minMagnitude = q.magnitude;
                    if (q.magnitude > maxMagnitude) maxMagnitude = q.magnitude;
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
                    bitmap = new Bitmap(options.width, options.height);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception creating bitmap: " + e);
                    bitmap = null;
                    Environment.Exit(1);
                }

                Console.WriteLine("Initializing blank screen");
                for (int y = 0; y < options.height; y ++)
                {
                    for (int x = 0; x < options.width; x ++)
                    {
                        bitmap.SetPixel(x, y, Constants.bgColor);
                    }
                }

                Console.WriteLine("Setting shores");
                StreamReader shores = OpenStream(options.shoreData);
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
                    Point p = new Point(new Position(lon, lat));
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

                Console.WriteLine("Drawing meridians of longitude");
                for (int ilon = -180; ilon <= 180; ilon += 15)
                {
                    for ( Position pos = new Position((double)ilon, 90.0);
                          ! pos.isNull;
                          pos = pos.NextPixelSouth())
                    {
                        Point p = new Point(pos);
                        if (p.x < options.width && p.y < options.height)
                        {
                            // Console.WriteLine("Plot " + p);
                            bitmap.SetPixel(p.x, p.y, Constants.axisColor);
                        }
                    }
                }

                Console.WriteLine("Drawing parallels of latitude");
                for (int ilat = -90; ilat <= 90; ilat += 15)
                {
                    for ( Position pos = new Position(-180.0, (double)ilat);
                          ! pos.isNull;
                          pos = pos.NextPixelEast())
                    {
                        Point p = new Point(pos);
                        if (p.x < options.width && p.y < options.height)
                        {
                            bitmap.SetPixel(p.x, p.y, Constants.axisColor);
                        }
                    }
                }

                Console.WriteLine("Iterating over quakes");
                foreach (Quake q in quakes)
                {
                    q.Plot(bitmap);
                }

                SaveBitmapToPng(bitmap, options.imageFile);
            }
        } // Main
    } // class Program
} // namespace Quakemap
