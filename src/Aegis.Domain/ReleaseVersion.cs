using System.Text.RegularExpressions;
namespace Aegis.Domain;

public sealed record ReleaseVersion(int Major,int Minor,int Patch,int? Beta) : IComparable<ReleaseVersion>
{
    public static ReleaseVersion? Parse(string value)
    {
        var m=Regex.Match(value,"^v?(\\d+)\\.(\\d+)\\.(\\d+)(?:-beta\\.(\\d+))?$",RegexOptions.CultureInvariant);
        if(!m.Success||!int.TryParse(m.Groups[1].Value,out int a)||!int.TryParse(m.Groups[2].Value,out int b)||!int.TryParse(m.Groups[3].Value,out int c))return null;
        int? beta=null;if(m.Groups[4].Success){if(!int.TryParse(m.Groups[4].Value,out int d))return null;beta=d;}
        return new(a,b,c,beta);
    }
    public int CompareTo(ReleaseVersion? other)
    {
        if(other is null)return 1;
        int result=Major.CompareTo(other.Major);if(result!=0)return result;
        result=Minor.CompareTo(other.Minor);if(result!=0)return result;
        result=Patch.CompareTo(other.Patch);if(result!=0)return result;
        if(Beta is null)return other.Beta is null?0:1;
        return other.Beta is null?-1:Beta.Value.CompareTo(other.Beta.Value);
    }
}
