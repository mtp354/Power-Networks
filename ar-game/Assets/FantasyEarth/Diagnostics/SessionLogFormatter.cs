using System.Globalization;
using System.Text;

namespace FantasyEarth.Diagnostics
{
    public static class SessionLogFormatter
    {
        public static string ToJsonLine(SessionLogEntry e)
        {
            var json = new StringBuilder(256);
            json.Append("{\"t\":").Append(e.UnixTimeMs.ToString(CultureInfo.InvariantCulture));
            json.Append(",\"lat\":");
            if (e.Position.HasValue)
            {
                AppendNumber(json, e.Position.Value.LatitudeDeg, "F7");
                json.Append(",\"lon\":");
                AppendNumber(json, e.Position.Value.LongitudeDeg, "F7");
            }
            else
            {
                json.Append("null,\"lon\":null");
            }
            json.Append(",\"hAcc\":");
            AppendNumber(json, e.HorizontalAccuracyM, "F2");
            json.Append(",\"yawAcc\":");
            AppendNumber(json, e.YawAccuracyDeg, "F2");
            json.Append(",\"earth\":");
            AppendString(json, e.EarthState);
            json.Append(",\"tracking\":").Append(e.IsTracking ? "true" : "false");
            json.Append(",\"alignment\":");
            AppendString(json, e.Alignment.ToString());
            json.Append(",\"blend\":");
            AppendNumber(json, e.RebuiltBlend, "F2");
            json.Append(",\"koppen\":");
            AppendString(json, e.Koppen.ToString());
            json.Append(",\"biome\":");
            AppendString(json, e.Biome.ToString());
            return json.Append('}').ToString();
        }

        private static void AppendNumber(StringBuilder json, double value, string format)
        {
            json.Append(double.IsNaN(value) || double.IsInfinity(value)
                ? "null"
                : value.ToString(format, CultureInfo.InvariantCulture));
        }

        private static void AppendString(StringBuilder json, string value)
        {
            if (value == null)
            {
                json.Append("null");
                return;
            }

            json.Append('"');
            foreach (char c in value)
            {
                switch (c)
                {
                    case '"': json.Append("\\\""); break;
                    case '\\': json.Append("\\\\"); break;
                    case '\b': json.Append("\\b"); break;
                    case '\f': json.Append("\\f"); break;
                    case '\n': json.Append("\\n"); break;
                    case '\r': json.Append("\\r"); break;
                    case '\t': json.Append("\\t"); break;
                    default:
                        if (c < 0x20)
                        {
                            json.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                        }
                        else
                        {
                            json.Append(c);
                        }
                        break;
                }
            }
            json.Append('"');
        }
    }
}
