using PoemClient.Source.View;
using PoemClientWPF;
using PoemClientWPF.Tools;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;

namespace PoemClient.Source.Tools
{
    public static class HEREMaps
    {

        public static async Task<List<LatLngZ>> GetTruckRouteFromHereApiAsync(
            string origin,
            string destination,
            double weightKg,
            double heightM,
            double widthM,
            double lengthM,
            string risk,
            string categoryTunnel,
            int nbTrailer,
            MainWindow mainWindow)
        {

            bool ok = KeyInputWindow.EnsureValidKeys(
            KeyInputMode.HereMaps,
            mainWindow,
            MainWindow.GetLineBelowParameter(mainWindow.clientConfig, "HereMapsKey"),
            MainWindow.GetLineBelowParameter(mainWindow.clientConfig, "HereMapsPassword"),
            (theApiKey, _) => KeyInputWindow.TestHereMapsCredentials(theApiKey, mainWindow),
            out string apiKey,
            out string _
            );

            if (!ok)
                return null;

            string[] hazard = { "combustible", "corrosive", "explosive", "flammable", "gas", "organic", "poison", "radioactive", "harmfulToWater", "other", "poisonousInhalation" };
            string hazardousGoods = !hazard.Contains(risk) ? "" : risk;

            string baseUrl = "https://router.hereapi.com/v8/routes";
            string query = "transportMode=truck" +
               "&origin=" + origin +
               "&destination=" + destination +
               "&return=polyline" +
               "&apiKey=" + apiKey +
               "&truck[height]=" + heightM.ToString(CultureInfo.InvariantCulture) +
               "&truck[width]=" + widthM.ToString(CultureInfo.InvariantCulture) +
               "&truck[length]=" + lengthM.ToString(CultureInfo.InvariantCulture) +
               "&truck[grossWeight]=" + weightKg.ToString(CultureInfo.InvariantCulture);

            if (categoryTunnel != "") query += "&truck[tunnelCategory]=" + categoryTunnel;
            if (hazardousGoods != "") query += "&truck[shippedHazardousGoods]=" + hazardousGoods;
            if (nbTrailer > 0) query += "&truck[trailerCount]=" + nbTrailer;

            string url = baseUrl + "?" + query;
            Console.WriteLine("url here : " + url);

            using (var client = new HttpClient())
            {
                try
                {
                    var response = await client.GetAsync(url);
                    if ((int)response.StatusCode == 429)
                    {
                        System.Windows.MessageBox.Show(mainWindow, MainWindow.config.GetKeyValue("DataSizeError"), MainWindow.config.GetKeyValue("Error"), MessageBoxButton.OK, MessageBoxImage.Error);
                        return null;
                    }
                    if (!response.IsSuccessStatusCode)
                    {
                        System.Windows.MessageBox.Show("Erreur HTTP HERE : " + response.StatusCode + " - " + response.ReasonPhrase);
                        return null;
                    }

                    string json = await response.Content.ReadAsStringAsync();
                    Console.WriteLine("Réponse JSON brute :\n" + json);

                    using (JsonDocument doc = JsonDocument.Parse(json))
                    {
                        var root = doc.RootElement;

                        if (!root.TryGetProperty("routes", out var routes) || routes.GetArrayLength() == 0)
                        {
                            System.Windows.MessageBox.Show("Aucune route trouvée dans la réponse HERE.");
                            return null;
                        }

                        var firstRoute = routes[0];

                        if (!firstRoute.TryGetProperty("sections", out var sections) || sections.GetArrayLength() == 0)
                        {
                            System.Windows.MessageBox.Show("Aucune section trouvée dans la première route.");
                            return null;
                        }

                        var firstSection = sections[0];

                        if (!firstSection.TryGetProperty("polyline", out var polylineProp))
                        {
                            System.Windows.MessageBox.Show("Pas de polyline trouvée dans la section.");
                            return null;
                        }

                        string polyline = polylineProp.GetString();
                        Console.WriteLine("Polyline : " + polyline);

                        List<LatLngZ> path = DecodePolyline(polyline);
                        if (path == null || path.Count == 0)
                        {
                            System.Windows.MessageBox.Show("Erreur de décodage de la polyline.");
                            return null;
                        }
                        return path;
                    }
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show("Exception lors de l'appel HERE : " + ex.Message);
                    Console.WriteLine("Exception détaillée : " + ex);
                    return null;
                }
            }
        }

        private static async Task<string> ReverseGeocodeAsync(MainWindow main, double lat, double lng)
        {
            string apiKey = MainWindow.GetLineBelowParameter(main.clientConfig, "HereMapsKey");
            string url = $"https://revgeocode.search.hereapi.com/v1/revgeocode?at={lat.ToString(CultureInfo.InvariantCulture)},{lng.ToString(CultureInfo.InvariantCulture)}&lang=fr&apiKey={apiKey}";
            try
            {
                using (var client = new HttpClient())
                {
                    var response = await client.GetAsync(url);
                    if (!response.IsSuccessStatusCode) return "";
                    string json = await response.Content.ReadAsStringAsync();
                    using (JsonDocument doc = JsonDocument.Parse(json))
                    {
                        var root = doc.RootElement;
                        if (root.TryGetProperty("items", out var items) && items.GetArrayLength() > 0)
                        {
                            var first = items[0];
                            if (first.TryGetProperty("address", out var address))
                            {
                                if (address.TryGetProperty("label", out var label))
                                    return label.GetString() ?? "";
                            }
                        }
                    }
                }
            }
            catch { }
            return "";
        }

        private static async Task<List<string>> ResolveLocationNamesAsync(MainWindow main, List<string> names, List<string> coordStrings)
        {
            var resolved = new List<string>();
            for (int i = 0; i < names.Count; i++)
            {
                if (!string.IsNullOrWhiteSpace(names[i]))
                {
                    resolved.Add(names[i]);
                }
                else if (i < coordStrings.Count && TryParseLatLng(coordStrings[i], out double lat, out double lng))
                {
                    string geocoded = await ReverseGeocodeAsync(main, lat, lng);
                    resolved.Add(geocoded);
                }
                else
                {
                    resolved.Add("");
                }
            }
            return resolved;
        }

        private static async Task<List<List<string>>> ResolveNestedLocationNamesAsync(MainWindow main, List<List<string>> allNames, List<List<string>> allCoords)
        {
            var result = new List<List<string>>();
            for (int i = 0; i < allNames.Count; i++)
            {
                var names = allNames != null && i < allNames.Count ? allNames[i] : new List<string>();
                var coords = allCoords != null && i < allCoords.Count ? allCoords[i] : new List<string>();

                int count = Math.Max(names.Count, coords.Count);
                var paddedNames = new List<string>();
                var paddedCoords = new List<string>();
                for (int j = 0; j < count; j++)
                {
                    paddedNames.Add(j < names.Count ? names[j] : "");
                    paddedCoords.Add(j < coords.Count ? coords[j] : "");
                }

                result.Add(await ResolveLocationNamesAsync(main, paddedNames, paddedCoords));
            }
            return result;
        }

        public static async Task<string> GenerateLeafletMapHtmlMultipleAsync(MainWindow main, List<List<LatLngZ>> allPaths, List<string> vehicleIds = null, List<List<string>> intermediates = null,
            List<List<string>> locationNames = null, Config cfg = null)
        {
            List<List<string>> resolvedNames = await ResolveNestedLocationNamesAsync(
                main,
                locationNames ?? allPaths.Select(_ => new List<string>()).ToList(),
                intermediates ?? allPaths.Select(_ => new List<string>()).ToList()
            );

            return GenerateLeafletMapHtmlMultiple(main, allPaths, vehicleIds, intermediates, resolvedNames, cfg);
        }

        public static string GenerateLeafletMapHtmlMultiple(MainWindow main, List<List<LatLngZ>> allPaths, List<string> vehicleIds = null, List<List<string>> intermediates = null,
            List<List<string>> locationNames = null, Config cfg = null)
        {
            if (allPaths == null || allPaths.Count == 0)
            {
                System.Windows.MessageBox.Show("Aucune route à afficher.", "Erreur",
                                               MessageBoxButton.OK, MessageBoxImage.Warning);
                return null;
            }

            var sbArrays = new StringBuilder();
            for (int i = 0; i < allPaths.Count; i++)
            {
                var path = allPaths[i];
                var sbPoints = new StringBuilder();
                foreach (var p in path)
                    sbPoints.AppendFormat(CultureInfo.InvariantCulture, "[{0}, {1}],", p.Lat, p.Lng);
                if (sbPoints.Length > 0) sbPoints.Length--;
                sbArrays.AppendLine($"var latlngs_{i} = [{sbPoints}];");
            }

            string[] colors = new[] { "#003366", "#ff7f50", "#2e8b57", "#8a2be2", "#ff4500", "#1e90ff", "#a52a2a", "#228b22" };
            var colorsJs = "[" + string.Join(",", colors.Select(c => $"'{c}'")) + "]";

            string centerLat = allPaths[0][0].Lat.ToString(CultureInfo.InvariantCulture);
            string centerLng = allPaths[0][0].Lng.ToString(CultureInfo.InvariantCulture);

            string html = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"" />
    <title>Multi-Itinéraires</title>
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <link rel=""stylesheet"" href=""https://unpkg.com/leaflet@1.9.4/dist/leaflet.css"" />
    <script src=""https://unpkg.com/leaflet@1.9.4/dist/leaflet.js""></script>
    <style>
        body {{ margin:0; padding:0; }}
        #map {{ height:100vh; width:100%; }}
        .badge-blue {{ background:#1976d2; color:#fff; border-radius:50%; width:26px; height:26px; text-align:center; line-height:26px; border:2px solid white; box-shadow:0 2px 4px rgba(0,0,0,0.3); font-weight:700; user-select:none; }}
        .legend {{ position: absolute; right: 10px; top: 10px; background: rgba(255,255,255,0.9); padding:8px; border-radius:6px; z-index:999; font-size:13px; }}
        .legend-item {{ margin-bottom:6px; display:flex; align-items:center; gap:8px; }}
    </style>
</head>
<body>
<div id=""map""></div>
<div class=""legend"" id=""legend""></div>

<script>
    var map = L.map('map', {{attributionControl: false }}).setView([{centerLat}, {centerLng}], 10);

    L.tileLayer('https://{{s}}.basemaps.cartocdn.com/rastertiles/voyager/{{z}}/{{x}}/{{y}}{{r}}.png', {{
        maxZoom: 19,
        attribution: ''
    }}).addTo(map);

    {sbArrays}

    var colors = {colorsJs};
    var boundsGroup = [];
    var allLocationNames = {GetJsNestedArray(locationNames)};

    function makeDivIcon(html, size) {{
        return L.divIcon({{ html: html, iconSize: size, className: '' }});
    }}

    for (let i = 0; i < {allPaths.Count}; i++) {{
        let pts = window['latlngs_' + i];
        let color = colors[i % colors.length];
        let vehicleName = ( ( {GetJsStringArray(vehicleIds)} )[i] ) || ('Tour ' + (i+1));

        let poly = L.polyline(pts, {{ color: color, weight: 5, opacity: 0.9 }}).addTo(map);
        boundsGroup.push(poly.getBounds());

        let s = pts[0];
        let startHtml = '<div style=""background:' + color + '; color:white; border-radius:50%; width:26px; height:26px; text-align:center; line-height:26px; font-size:16px;"">🚩</div>';
        L.marker(s, {{ icon: makeDivIcon(startHtml, [26,26]) }}).addTo(map)
            .bindTooltip('🚩 Départ - ' + vehicleName);

        let e = pts[pts.length - 1];
        let endHtml = '<div style=""background:#ff4444; color:white; border-radius:50%; width:26px; height:26px; text-align:center; line-height:26px; font-size:16px;"">🏁</div>';
        L.marker(e, {{ icon: makeDivIcon(endHtml, [26,26]) }}).addTo(map)
            .bindTooltip('🏁 Arrivée - ' + vehicleName);

        let inter = ( ( {GetJsNestedArray(intermediates)} ) || [] )[i] || [];
        let names = ( allLocationNames[i] ) || [];

        for (let j = 0; j < inter.length; j++) {{
            let parts = inter[j].split(',');
            let lat = parseFloat(parts[0]), lng = parseFloat(parts[1]);
            let iconHtml = '<div class=""badge-blue"">' + (j + 1) + '</div>';
            L.marker([lat, lng], {{ icon: makeDivIcon(iconHtml, [26,26]) }})
                .addTo(map)
                .bindTooltip(names[j] || '');
        }}

        var legend = document.getElementById('legend');
        var item = document.createElement('div');
        item.className = 'legend-item';
        item.innerHTML = '<div style=""width:18px;height:12px;background:' + color + ';border-radius:3px;""></div><div>' + vehicleName + '</div>';
        legend.appendChild(item);
    }}

    if (boundsGroup.length > 0) {{
        var group = boundsGroup[0];
        for (var k = 1; k < boundsGroup.length; k++) group = group.extend(boundsGroup[k]);
        map.fitBounds(group.pad(0.2));
    }}
</script>
</body>
</html>
";
            return html;
        }

        private static string GetJsStringArray(List<string> items)
        {
            if (items == null) return "[]";
            var cleaned = items.Select(s => s == null ? "" : s.Replace("'", "\\'"));
            return "['" + string.Join("','", cleaned) + "']";
        }

        private static string GetJsNestedArray(List<List<string>> nested)
        {
            if (nested == null) return "[]";
            var parts = nested.Select(list =>
            {
                if (list == null) return "[]";
                var inner = list.Select(s => {
                    var cs = s.Replace("'", "\\'");
                    return $"'{cs}'";
                });
                return "[" + string.Join(",", inner) + "]";
            });
            return "[" + string.Join(",", parts) + "]";
        }

        public static string GenerateLeafletMapHtml(MainWindow main, List<LatLngZ> path, Config cfg = null)
        {
            return GenerateLeafletMapHtml(main, path, null, null, cfg);
        }

        public static async Task<string> GenerateLeafletMapHtmlAsync(MainWindow main, List<LatLngZ> path, List<string> intermediate, List<string> locationNames, Config cfg = null)
        {
            List<string> resolvedNames = locationNames != null
                ? await ResolveLocationNamesAsync(main, locationNames, intermediate ?? new List<string>())
                : new List<string>();

            return GenerateLeafletMapHtml(main, path, intermediate, resolvedNames, cfg);
        }

        public static string GenerateLeafletMapHtml(MainWindow main, List<LatLngZ> path, List<string> intermediate, List<string> locationNames, Config cfg = null)
        {
            if (path == null || path.Count < 2)
            {
                System.Windows.MessageBox.Show("Le chemin doit contenir au moins deux points.",
                                               "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
                return null;
            }

            var sbPoints = new StringBuilder();
            foreach (var point in path)
                sbPoints.AppendFormat(CultureInfo.InvariantCulture, "[{0}, {1}],", point.Lat, point.Lng);
            if (sbPoints.Length > 0) sbPoints.Length--;

            var intermediatesLatLng = new List<LatLngZ>();
            if (intermediate != null)
            {
                foreach (var s in intermediate)
                {
                    if (TryParseLatLng(s, out double lat, out double lng))
                        intermediatesLatLng.Add(new LatLngZ(lat, lng));
                }
            }

            var sbInter = new StringBuilder();
            foreach (var p in intermediatesLatLng)
                sbInter.AppendFormat(CultureInfo.InvariantCulture, "[{0}, {1}],", p.Lat, p.Lng);
            if (sbInter.Length > 0) sbInter.Length--;

            string namesJs = "[]";
            if (locationNames != null && locationNames.Count > 0)
            {
                var escapedNames = locationNames.Select(n =>
                    n != null ? n.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("'", "\\'") : ""
                );
                namesJs = "['" + string.Join("','", escapedNames) + "']";
            }

            string centerLat = path[0].Lat.ToString(CultureInfo.InvariantCulture);
            string centerLng = path[0].Lng.ToString(CultureInfo.InvariantCulture);
            var startPoint = path[0];
            var endPoint = path[path.Count - 1];

            string startLat = startPoint.Lat.ToString(CultureInfo.InvariantCulture);
            string startLng = startPoint.Lng.ToString(CultureInfo.InvariantCulture);
            string endLat = endPoint.Lat.ToString(CultureInfo.InvariantCulture);
            string endLng = endPoint.Lng.ToString(CultureInfo.InvariantCulture);

            string htmlContent = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"" />
    <title>Itinéraire</title>
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <link rel=""stylesheet"" href=""https://unpkg.com/leaflet@1.9.4/dist/leaflet.css"" />
    <script src=""https://unpkg.com/leaflet@1.9.4/dist/leaflet.js""></script>
    <style>
        body {{ margin: 0; padding: 0; }}
        #map {{ height: 100vh; width: 100%; }}
        .badge-blue {{
            background:#1976d2;
            color:#fff;
            border-radius:50%;
            width:26px; height:26px;
            text-align:center;
            line-height:26px;
            border:2px solid white;
            box-shadow:0 2px 4px rgba(0,0,0,0.3);
            font-weight:700;
            user-select:none;
        }}
    </style>
</head>
<body>
<div id=""map""></div>
<script>
    var map = L.map('map', {{ attributionControl: false }}).setView([{centerLat}, {centerLng}], 13);

    L.tileLayer('https://{{s}}.basemaps.cartocdn.com/rastertiles/voyager/{{z}}/{{x}}/{{y}}{{r}}.png', {{
        maxZoom: 19,
        attribution: ''
    }}).addTo(map);

    var latlngs = [{sbPoints}];
    var polyline = L.polyline(latlngs, {{
        color: '#003366',
        weight: 5,
        opacity: 0.9
    }}).addTo(map);

    var locationNames = {namesJs};

    var startIcon = L.divIcon({{
        html: '<div style=""background:#00c851; color:white; border-radius:50%; width:25px; height:25px; text-align:center; line-height:25px;"">🚩</div>',
        iconSize: [25, 25], iconAnchor: [12, 12]
    }});
    L.marker([{startLat}, {startLng}], {{ icon: startIcon }}).addTo(map)
        .bindTooltip('🚩 Départ');

    var endIcon = L.divIcon({{
        html: '<div style=""background:#ff4444; color:white; border-radius:50%; width:25px; height:25px; text-align:center; line-height:25px;"">🏁</div>',
        iconSize: [25, 25], iconAnchor: [12, 12]
    }});
    L.marker([{endLat}, {endLng}], {{ icon: endIcon }}).addTo(map)
        .bindTooltip('🏁 Arrivée');

    var intermediates = [{sbInter}];
    intermediates.forEach(function(coord, idx) {{
        var icon = L.divIcon({{
            html: '<div class=""badge-blue"">' + (idx + 1) + '</div>',
            iconSize: [26, 26], iconAnchor: [13, 13]
        }});
        L.marker(coord, {{ icon: icon }}).addTo(map)
            .bindTooltip(locationNames[idx] || '');
    }});

    setTimeout(function() {{
        map.fitBounds(polyline.getBounds().pad(0.2));
    }}, 300);
</script>
</body>
</html>";
            return htmlContent;
        }

        private static bool TryParseLatLng(string s, out double lat, out double lng)
        {
            lat = 0; lng = 0;
            if (string.IsNullOrWhiteSpace(s)) return false;

            string raw = Uri.UnescapeDataString(s).Trim();
            var parts = raw.Split(new[] { ',', ';', '|', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2) return false;

            if (double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out lat) &&
                double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out lng))
                return true;

            if (double.TryParse(parts[0].Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out lat) &&
                double.TryParse(parts[1].Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out lng))
                return true;

            return false;
        }

        public static string GenerateSinglePointMapScript(LatLngZ point)
        {
            string lat = point.Lat.ToString(CultureInfo.InvariantCulture);
            string lng = point.Lng.ToString(CultureInfo.InvariantCulture);

            string htmlContent = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"" />
    <title>Carte Leaflet - Point unique</title>
    <link rel=""stylesheet"" href=""https://unpkg.com/leaflet@1.9.3/dist/leaflet.css"" />
    <style>
        html, body {{ height: 100%; margin: 0; padding: 0; }}
        #map {{ height: 100%; width: 100%; }}
    </style>
</head>
<body>
    <div id=""map""></div>
    <script src=""https://unpkg.com/leaflet@1.9.3/dist/leaflet.js""></script>
    <script>
        var map = L.map('map').setView([{lat}, {lng}], 13);

    L.tileLayer('https://{{s}}.basemaps.cartocdn.com/rastertiles/voyager/{{z}}/{{x}}/{{y}}{{r}}.png', {{
        attribution: '© OpenStreetMap © CARTO'
    }}).addTo(map);

        L.marker([{lat}, {lng}]).addTo(map)
            .bindPopup('{lat}, {lng}').openPopup();
    </script>
</body>
</html>";

            return htmlContent;
        }

        public static List<LatLngZ> DecodePolyline(string encoded)
        {
            var decodedPoints = PoemClient.Source.Tools.PolylineEncoderDecoder.Decode(encoded);
            return decodedPoints;
        }
    }
}