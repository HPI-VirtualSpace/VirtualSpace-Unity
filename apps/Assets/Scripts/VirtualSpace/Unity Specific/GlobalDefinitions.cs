using System;
using System.Net;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;

namespace VirtualSpace
{
	public class BiddingDataLegacy
	{
	    public float[] bids;
        public BiddingDataLegacy() { } //: this(new float[GlobalVars.Grid.steps_X * GlobalVars.Grid.steps_Z]) { }
	    public BiddingDataLegacy(float[] bids)
	    {
	        this.bids = bids;
	    }
		public BiddingDataLegacy Clone()
		{
			return new BiddingDataLegacy(bids);
		}
		public float get(int x, int z)
		{
            return 0;//bids[x * GlobalVars.Grid.steps_Z + z];
		}
	}

	public class PlayerPositionLegacy
	{
		public float position_X = 0.0f;
		public float position_Z = 0.0f;
		public float orientation_X = 0.0f;
		public float orientation_Z = 0.0f;
		public PlayerPositionLegacy(){}
		public PlayerPositionLegacy(float px, float pz, float ox, float oz)
		{
			position_X = px;
			position_Z = pz;
			orientation_X = ox;
			orientation_Z = oz;
		}
		public PlayerPositionLegacy(float[] f)
		{
			position_X = f[0];
			position_Z = f[1];
			orientation_X = f[2];
			orientation_Z = f[3];
		}
		public PlayerPositionLegacy Clone()
		{
			return new PlayerPositionLegacy(position_X, position_Z, orientation_X, orientation_Z);
		}
		public override string ToString()
		{
			return position_X.ToString(CultureInfo.InvariantCulture) + "|" + position_Z.ToString(CultureInfo.InvariantCulture) + "|" + orientation_X.ToString(CultureInfo.InvariantCulture) + "|" + orientation_Z.ToString(CultureInfo.InvariantCulture);
		}
		public static PlayerPositionLegacy FromString(string s)
		{
			string[] split = s.Split('|');
			return new PlayerPositionLegacy(split.Select(str => float.Parse(str, CultureInfo.InvariantCulture)).ToArray());
		}
	}

	public class RectLegacy
	{
		public float lower_X;
		public float lower_Z;
		public float upper_X;
		public float upper_Z;
		public float startTime;
		public float endTime;
		public RectLegacy(float lx, float lz, float ux, float uz, float st, float et)
		{
			lower_X = lx;
			lower_Z = lz;
			upper_X = ux;
			upper_Z = uz;
			startTime = st;
			endTime = et;
		}
		public RectLegacy(float[] f)
		{
			lower_X = f[0];
			lower_Z = f[1];
			upper_X = f[2];
			upper_Z = f[3];
			startTime = (int)f[4];
			endTime = (int)f[5];
		}
		public override string ToString()
		{
			return lower_X.ToString(CultureInfo.InvariantCulture) + "|" + lower_Z.ToString(CultureInfo.InvariantCulture) + "|" + upper_X.ToString(CultureInfo.InvariantCulture) + "|" + upper_Z.ToString(CultureInfo.InvariantCulture) + "|" + startTime.ToString(CultureInfo.InvariantCulture) +"|" + endTime.ToString(CultureInfo.InvariantCulture);
		}
		public static RectLegacy FromString(string s)
		{
			string[] split = s.Split('|');
			return new RectLegacy(split.Select(str => float.Parse(str, CultureInfo.InvariantCulture)).ToArray());
		}
	}

	public class SpaceRequirementLegacy
	{
		public RectLegacy rect;
		public float incentive;
		public SpaceRequirementLegacy(float lx, float lz, float ux, float uz, float ft, float lt, float i)
		{
			rect = new RectLegacy(lx, lz, ux, uz, ft, lt);
			incentive = i;
		}
		public SpaceRequirementLegacy(float[] f)
		{
			rect = new RectLegacy(f);
			incentive = f[6];
		}
		public override string ToString()
		{
			return rect.ToString() + "|" + incentive.ToString(CultureInfo.InvariantCulture);
		}
		public static SpaceRequirementLegacy FromString(string s)
		{
			string[] split = s.Split('|');
			return new SpaceRequirementLegacy(split.Select(str => float.Parse(str, CultureInfo.InvariantCulture)).ToArray());
		}
		public SpaceRequirementLegacy Clone()
		{
			return new SpaceRequirementLegacy(rect.lower_X, rect.lower_Z, rect.upper_X, rect.upper_Z, rect.startTime, rect.endTime, incentive);
		}
	}

	public class SpaceRequirementListLegacy
	{
		public List<SpaceRequirementLegacy> data;
		public bool overwrite = false;
		public SpaceRequirementListLegacy()
		{
			data = new List<SpaceRequirementLegacy>();
		}
		public SpaceRequirementListLegacy(bool overwrite)
		{
			this.overwrite = overwrite;
			data = new List<SpaceRequirementLegacy>();
		}
		public SpaceRequirementListLegacy(SpaceRequirementLegacy[] s)
		{
			data = new List<SpaceRequirementLegacy>(s);
		}
		public SpaceRequirementListLegacy(SpaceRequirementLegacy[] s, bool overwrite)
		{
			this.overwrite = overwrite;
			data = new List<SpaceRequirementLegacy>(s);
		}
		public void Add(SpaceRequirementLegacy s)
		{
			data.Add(s);
		}
		public override string ToString()
		{
			string s = overwrite ? "1" : "0";
			data.ForEach(delegate(SpaceRequirementLegacy sr)
			{
				s += sr.ToString() + "#";
			});
			return s.Trim('#');
		}
		public static SpaceRequirementListLegacy FromString(string s)
		{
			bool overwrite = s.ElementAt(0) == 1;
			s = s.Remove(0, 1);
			if(s.Length > 0)
			{
				string[] split = s.Split('#');
				return new SpaceRequirementListLegacy(split.Select(str => SpaceRequirementLegacy.FromString(str)).ToArray(), overwrite);
			}
			else
			{
				return new SpaceRequirementListLegacy(overwrite);
			}
		}
	}

	public class UnavailableSpacePointLegacy
	{
		public float x;
		public float z;
		public float t;
		public float dist;
		public UnavailableSpacePointLegacy(float x, float z, float t, float dist)
		{
			this.x = x;
			this.z = z;
			this.t = t;
			this.dist = dist;
		}
		public UnavailableSpacePointLegacy(float[] f)
		{
			this.x = f[0];
			this.z = f[1];
			this.t = f[2];
			this.dist = f[3];
		}
		public override string ToString()
		{
			return x.ToString(CultureInfo.InvariantCulture) + "|" + z.ToString(CultureInfo.InvariantCulture) + "|" + t.ToString(CultureInfo.InvariantCulture) + "|" + dist.ToString(CultureInfo.InvariantCulture);
		}
		public static UnavailableSpacePointLegacy FromString(string s)
		{
			string[] split = s.Split('|');
			return new UnavailableSpacePointLegacy(split.Select(str => float.Parse(str, CultureInfo.InvariantCulture)).ToArray());
		}
	}

	public class PointLegacy
	{
		public float x;
		public float z;
		public PointLegacy(float x, float z)
		{
			this.x = x;
			this.z = z;
		}
		public PointLegacy(float[] f)
		{
			this.x = f[0];
			this.z = f[1];
		}
		public override string ToString()
		{
			return x.ToString(CultureInfo.InvariantCulture) + "|" + z.ToString(CultureInfo.InvariantCulture);
		}
		public static PointLegacy FromString(string s)
		{
			string[] split = s.Split('|');
			return new PointLegacy(split.Select(str => float.Parse(str, CultureInfo.InvariantCulture)).ToArray());
		}
	}
	
	public class UnavailableSpaceListLegacy
	{
		public List<RectLegacy> data;
		public UnavailableSpacePointLegacy closestUnavailablePosition;
		public PointLegacy optimalPosition;
		public UnavailableSpaceListLegacy()
		{
			data = new List<RectLegacy>();
		}
		public UnavailableSpaceListLegacy(RectLegacy[] s)
		{
			data = new List<RectLegacy>(s);
		}
		public UnavailableSpaceListLegacy(UnavailableSpacePointLegacy usp)
		{
			data = new List<RectLegacy>();
			closestUnavailablePosition = usp;
		}
		public UnavailableSpaceListLegacy(UnavailableSpacePointLegacy usp, PointLegacy op)
		{
			data = new List<RectLegacy>();
			closestUnavailablePosition = usp;
			optimalPosition = op;
		}
		public UnavailableSpaceListLegacy(RectLegacy[] s, UnavailableSpacePointLegacy usp)
		{
			data = new List<RectLegacy>(s);
			closestUnavailablePosition = usp;
		}
		public UnavailableSpaceListLegacy(RectLegacy[] s, UnavailableSpacePointLegacy usp, PointLegacy op)
		{
			data = new List<RectLegacy>(s);
			closestUnavailablePosition = usp;
			optimalPosition = op;
		}
		public void Add(RectLegacy s)
		{
			data.Add(s);
		}
		public override string ToString()
		{
			string s = "";
			if(closestUnavailablePosition != null)
			{
				string cup = closestUnavailablePosition.ToString();
				string length = cup.Length.ToString("D8");
				s += length + cup;
			}
			else
			{
				s += 0.ToString("D8");
			}
			if(optimalPosition != null)
			{
				string op = optimalPosition.ToString();
				string length = op.Length.ToString("D8");
				s += length + op;
			}
			else
			{
				s += 0.ToString("D8");
			}
			data.ForEach(delegate(RectLegacy r)
			{
				s += r.ToString() + "#";
			});
			return s.Trim('#');
		}
		public static UnavailableSpaceListLegacy FromString(string s)
		{
			int readingPos = 0; // skip header
            int length = Int32.Parse(s.Substring(readingPos, 8));
			readingPos += 8;
			UnavailableSpacePointLegacy closestUnavailablePosition = length > 0 ? UnavailableSpacePointLegacy.FromString(s.Substring(readingPos, length)) : null;
			readingPos += length;
            length = Int32.Parse(s.Substring(readingPos, 8));
			readingPos += 8;
			PointLegacy optimalPosition = length > 0 ? PointLegacy.FromString(s.Substring(readingPos, length)) : null;
			readingPos += length;
			s = s.Substring(readingPos);
			if(s.Length > 0)
			{
				string[] split = s.Split('#');
				return new UnavailableSpaceListLegacy(split.Select(str => RectLegacy.FromString(str)).ToArray(), closestUnavailablePosition, optimalPosition);
			}
			else
			{
				return new UnavailableSpaceListLegacy(closestUnavailablePosition, optimalPosition);
			}
		}
		
	}

    public class Tuple<T1, T2>
    {
        public T1 Item1 { get; private set; }
        public T2 Item2 { get; private set; }
        internal Tuple(T1 item1, T2 item2)
        {
            Item1 = item1;
            Item2 = item2;
        }
    }

    public class CrashDataLegacy
	{
		public Tuple<float, float> direction;
		public float distance;
		public CrashDataLegacy(float directionX, float directionZ, float distance)
		{
			direction = new Tuple<float, float>(directionX, directionZ);
			this.distance = distance;
		}
		public CrashDataLegacy(float[] a)
		{
		direction = new Tuple<float, float>(a[0], a[1]);
			this.distance = a[2];
		}
		public override string ToString()
		{
			return direction.Item1.ToString(CultureInfo.InvariantCulture) + "|" + direction.Item2.ToString(CultureInfo.InvariantCulture) + "|" + distance.ToString(CultureInfo.InvariantCulture);
		}
		public static CrashDataLegacy FromString(string s)
		{
			string[] split = s.Split('|');
			return new CrashDataLegacy(split.Select(str => float.Parse(str, CultureInfo.InvariantCulture)).ToArray());
		}
	}
}