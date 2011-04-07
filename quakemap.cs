using System;
using System.IO;
using System.Net;
using System.Text;
using System.Collections;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Drawing;
using System.Drawing.Imaging;

struct Constants
{
    public static CultureInfo enUS = new CultureInfo("en-US");
    public static DateTime now = DateTime.UtcNow;
    public static string url = "http://earthquake.usgs.gov/earthquakes/catalogs/eqs7day-M1.txt";
    public static string dateFormat = @"dddd, MMMM d, yyyy HH:mm:ss \U\T\C";
    public static string reverseVideo = "\x1b[3m";
    public static string bold         = "\x1b[1m";
    public static string plain        = "\x1b[m";
    public static TimeSpan oneHour = new TimeSpan(0, 1, 0, 0);
    public static TimeSpan oneDay  = new TimeSpan(1, 0, 0, 0);
    public static int width  = 2000;
    public static int height = 1000;
    public static string imageFile = "/home/kst/public_html/quakes.png";
    public static int black = Color.Black.ToArgb();
    public static int gray  = Color.Gray.ToArgb();
    public static int white = Color.White.ToArgb();
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
    public TimeSpan age; // in seconds

    public override string ToString()
    {
        return "Src=" + Src + ", Eqid=" + Eqid + ", Version=" + Version + 
               ", Datetime=" + Datetime + ", Lat=" + Lat + ", Lon=" + Lon +
               ", Magnitude=" + Magnitude + ", Depth=" + Depth +
               ", NST=" + NST + ", Region=" + Region +
               ", dt=" + dt.ToString("u", Constants.enUS) +
               ", age=" + age;
    }
}

public class WebTest
{
    static public Color magToColor(double magnitude)
    {
        // 1.0: green
        // 5.0: blue
        // 9.0: red
        // Intermediate values are interpolated
        int red, green, blue;
        if (magnitude <= 1.0)
        {
            return Color.Green;
        }
        else if (magnitude <= 5.0)
        {
            double x = (magnitude - 1.0) / 4.0; // 0.0..1.0
            blue = (int)(x * 255);
            green = 255 - blue;
            red = 0;
        }
        else if (magnitude <= 9.0)
        {
            double x = (magnitude - 5.0) / 4.0; // 0.0..1.0
            green = 0;
            red = (int)(x * 255);
            blue = 255 - red;
        }
        else
        {
            return Color.Red;
        }
        return Color.FromArgb(red, green, blue);
    }

    static public void Main()
    {
        WebRequest request = WebRequest.Create (Constants.url);

        // For HTTP, cast the request to HttpWebRequest
        // allowing setting more properties, e.g. User-Agent.
        // An HTTP response can be cast to HttpWebResponse.

        using (WebResponse response = request.GetResponse())
        {
            // Ensure that the correct encoding is used. 
            // Check the response for the Web server encoding.
            // For binary content, use a stream directly rather
            // than wrapping it with StreamReader.

            using (StreamReader reader = new StreamReader (response.GetResponseStream(), Encoding.ASCII))
            {
                string line1 = reader.ReadLine();
                string[] headers = line1.Split(new char[] {','}, StringSplitOptions.None);
                foreach (string header in headers)
                {
                    Console.WriteLine('"' + header + '"');
                }

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

                string tmp_line;
                ArrayList lines  = new ArrayList();
                ArrayList quakes = new ArrayList();
                while ((tmp_line = reader.ReadLine()) != null)
                {
                    lines.Add(tmp_line);
                    Match m = line_re.Match(tmp_line);
                    if (m.Success)
                    {
                        Quake q = new Quake();
                        // Console.WriteLine("m.Groups[0].Value = \"" + m.Groups[0].Value + "\"");
                        // Console.WriteLine("m.Groups[1].Value = \"" + m.Groups[1].Value + "\"");
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
                        // Console.WriteLine("q = " + q);
                        // Environment.Exit(42);
                        quakes.Add(q);
                    }
                    else
                    {
                        Console.WriteLine("Line \"" + tmp_line + "\" does not match Regex " + line_re);
                        Environment.Exit(1);
                    }
                }
                Console.WriteLine("Got " + lines.Count + " lines, " + quakes.Count + " earthquakes");

                double minLat = Double.MaxValue;
                double maxLat = Double.MinValue;
                double minLon = Double.MaxValue;
                double maxLon = Double.MinValue;
                double minMagnitude = Double.MaxValue;
                double maxMagnitude = Double.MinValue;
                TimeSpan minAge = TimeSpan.MaxValue;
                TimeSpan maxAge = TimeSpan.MinValue;
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

                Bitmap bitmap = new Bitmap(Constants.width, Constants.height);

#if UNDEF
*               Console.WriteLine("Experiment: setting and getting one white pixel:");
*               {
*                   Console.WriteLine("Setting " + Color.White);
*                   bitmap.SetPixel(0, 0, Color.White);
*                   Console.WriteLine("Getting " + bitmap.GetPixel(0, 0));
*               }
#endif

                Console.WriteLine("Initializing blank screen");
                for (int y = 0; y < Constants.height; y ++)
                {
                    for (int x = 0; x < Constants.width; x ++)
                    {
                        bitmap.SetPixel(x, y, Color.White);
                    }
                }

                Console.WriteLine("Settings shores");
                StreamReader shores = new StreamReader("/home/kst/shores.txt");
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
                    int x = (int)((lon + 180.0) / 360.0 * Constants.width);
                    int y = (int)((lat +  90.0) / 180.0 * Constants.height);
                    y = Constants.height - y;
                    shorePoints ++;
                    if (bitmap.GetPixel(x, y).ToArgb() != Constants.gray)
                    {
                        bitmap.SetPixel(x, y, Color.Gray);
                        shorePixels ++;
                    }
                }
                Console.WriteLine("Plotted " + shorePixels + " pixels for " + shorePoints + " points");

                Console.WriteLine("Setting axes");
                for (int y = 0; y < Constants.height; y ++)
                {
                    bitmap.SetPixel(0,                 y, Color.LightGray);
                    bitmap.SetPixel(Constants.width/2, y, Color.LightGray);
                    bitmap.SetPixel(Constants.width-1, y, Color.LightGray);
                }
                for (int x = 0; x < Constants.width; x ++)
                {
                    bitmap.SetPixel(x, 0,                  Color.LightGray);
                    bitmap.SetPixel(x, Constants.height/2, Color.LightGray);
                    bitmap.SetPixel(x, Constants.height-1, Color.LightGray);
                }

#if UNDEF
*               Color[] sym = new Color[10];
*               for (int i = 0; i < 10; i ++)
*               {
*                   int red = i * 25;
*                   int green = 255 - red;
*                   int blue = 0;
*                   sym[i] = Color.FromArgb(red, green, blue);
*               }
#endif

                Console.WriteLine("Iterating over quakes");
                foreach (Quake q in quakes)
                {
                    // Console.Write('.');
                    // Scale Lon from (-180..+180) to (0..131)
                    // Scale Lat from (-90..+90) to (0..59)
                    int x = (int)((q.Lon + 180.0) / 360.0 * Constants.width);
                    int y = (int)((q.Lat +  90.0) / 180.0 * Constants.height);
                    y = Constants.height - y;
                    // Console.Write("x = " + x + ", y = " + y + ", mag = " + (int)q.Magnitude);
                    // Color symbol = sym[(int)q.Magnitude];
                 // if (! Char.IsDigit(screen[y,x][0]) || symbol > screen[y,x][0])
                 // {
                 //     if (q.age <= Constants.oneHour)
                 //     {
                 //         screen[y,x] = Constants.reverseVideo + symbol + Constants.plain;
                 //     }
                 //     else if (q.age <= Constants.oneDay)
                 //     {
                 //         screen[y,x] = Constants.bold + symbol + Constants.plain;
                 //     }
                 //     else
                 //     {
                 //         screen[y,x] = "" + symbol;
                 //     }
                 // }
                    // bitmap.SetPixel(x, y, symbol);
                    bitmap.SetPixel(x, y, magToColor(q.Magnitude));
                    // Console.WriteLine("screen[" + y + "," + x + "] = " + screen[y,x]);
                }
                Console.WriteLine("Saving to " + Constants.imageFile);
                bitmap.Save(Constants.imageFile, ImageFormat.Png);
                // Console.WriteLine();
                // Console.WriteLine("Done");
            }
        }
    }
}
