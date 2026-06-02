using System.Globalization;

namespace PoemClient.Source.Tools
{
    /// <summary>
    /// Coordinate triple
    /// </summary>
    public class LatLngZ
    {
        public double Lat { get; }
        public double Lng { get; }
        public double Z { get; }
        public string Name { get; }

        public LatLngZ(double latitude, double longitude, double thirdDimension = 0, string name = null)
        {
            Lat = latitude;
            Lng = longitude;
            Z = thirdDimension;
            Name = name ?? string.Empty;
        }

        public override string ToString()
        {
            if (!string.IsNullOrEmpty(Name))
                return $"LatLngZ [lat={Lat.ToString(CultureInfo.InvariantCulture)}, lng={Lng.ToString(CultureInfo.InvariantCulture)}, z={Z}, name={Name}]";
            return $"LatLngZ [lat={Lat.ToString(CultureInfo.InvariantCulture)}, lng={Lng.ToString(CultureInfo.InvariantCulture)}, z={Z}]";
        }


        public override bool Equals(object obj)
        {
            if (this == obj)
            {
                return true;
            }
            if (obj is LatLngZ latLngZ)
            {
                if (latLngZ.Lat == Lat && latLngZ.Lng == Lng && latLngZ.Z == Z)
                {
                    return true;
                }
            }
            return false;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + Lat.GetHashCode();
                hash = hash * 23 + Lng.GetHashCode();
                hash = hash * 23 + Z.GetHashCode();
                return hash;
            }
        }
    }
}