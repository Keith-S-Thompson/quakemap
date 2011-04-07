// $Id: quakemap.cs,v 1.12 2011/04/07 22:14:03 kst Exp $
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

struct Constants
{
    public static readonly CultureInfo enUS = new CultureInfo("en-US");
    public static readonly DateTime now = DateTime.UtcNow;
    public static readonly URL quakeData = new URL("http://earthquake.usgs.gov/earthquakes/catalogs/eqs7day-M1.txt");
    public static readonly URL shoreData = new URL("http://smov.org/~kst/shores.txt");
    public static readonly string dateFormat = @"dddd, MMMM d, yyyy HH:mm:ss \U\T\C";
//  public static readonly TimeSpan oneHour = new TimeSpan(0, 1, 0, 0);
//  public static readonly TimeSpan oneDay  = new TimeSpan(1, 0, 0, 0);
    public static readonly int width  = 1600;
    public static readonly int height =  800;
    public static readonly FileName imageFile = new FileName("/home/kst/public_html/quakes.png");
    public static readonly Color bgColor    = Color.White;
    public static readonly Color axisColor  = Color.Gray;
    public static readonly Color shoreColor = Color.Cyan;
    public static readonly int   bgARGB     = bgColor.ToArgb();
    public static readonly int   axisARGB   = axisColor.ToArgb();
    public static readonly int   shoreARGB  = shoreColor.ToArgb();
}

abstract class StringWrapper
{
    public string s;
    public StringWrapper()
    {
        this.s = null;
    }
    public StringWrapper(string s)
    {
        this.s = s;
    }
    public override string ToString()
    {
        return s;
    }
    public abstract StreamReader Open();
}

class FileName: StringWrapper
{
    public FileName(string s)
    {
        this.s = s;
    }
    public override StreamReader Open()
    {
        return new StreamReader(s);
    }
}

class URL: StringWrapper
{
    public URL(string s)
    {
        this.s = s;
    }
    public override StreamReader Open()
    {
        WebRequest request = WebRequest.Create(s);
        WebResponse response = request.GetResponse();
        return new StreamReader(response.GetResponseStream(), Encoding.ASCII);
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

public class QuakeMap
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
        Console.WriteLine("quakemap");

        using (StreamReader reader = Constants.quakeData.Open())
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

            Console.WriteLine("Initializing blank screen");
            for (int y = 0; y < Constants.height; y ++)
            {
                for (int x = 0; x < Constants.width; x ++)
                {
                    bitmap.SetPixel(x, y, Constants.bgColor);
                }
            }

            Console.WriteLine("Setting shores");
            StreamReader shores = Constants.shoreData.Open();
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
                if (bitmap.GetPixel(x, y).ToArgb() != Constants.shoreARGB)
                {
                    bitmap.SetPixel(x, y, Constants.shoreColor);
                    shorePixels ++;
                }
            }
            double percentage = (double)shorePixels / (double)shorePoints * 100.0;
            Console.WriteLine("Plotted " + shorePixels + " pixels for " + shorePoints + " points" +
                              " (" + percentage.ToString("0.##") + "%)");

            Console.WriteLine("Setting axes");
            for (int y = 0; y < Constants.height; y ++)
            {
                bitmap.SetPixel(0,                 y, Constants.axisColor);
                bitmap.SetPixel(Constants.width/2, y, Constants.axisColor);
                bitmap.SetPixel(Constants.width-1, y, Constants.axisColor);
            }
            for (int x = 0; x < Constants.width; x ++)
            {
                bitmap.SetPixel(x, 0,                  Constants.axisColor);
                bitmap.SetPixel(x, Constants.height/2, Constants.axisColor);
                bitmap.SetPixel(x, Constants.height-1, Constants.axisColor);
            }

            Console.WriteLine("Iterating over quakes");
            foreach (Quake q in quakes)
            {
                // Scale Lon from (-180..+180) to (0..131)
                // Scale Lat from (-90..+90) to (0..59)
                int x = (int)((q.Lon + 180.0) / 360.0 * Constants.width);
                int y = (int)((q.Lat +  90.0) / 180.0 * Constants.height);
                y = Constants.height - y;
                // Console.Write("x = " + x + ", y = " + y + ", mag = " + (int)q.Magnitude);
                bitmap.SetPixel(x, y, magToColor(q.Magnitude));
            }
            Console.WriteLine("Saving to " + Constants.imageFile);
            bitmap.Save(Constants.imageFile.s, ImageFormat.Png);
        }
    }
}
