using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml;

namespace bedu
{
	// - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	// B  E  F I L E 
	// - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	public class BEFile
	{
		string m_sName;
		string m_sFullName;
		long m_nBytes;

		public string Name { get { return m_sName;; } }
		public string FullName { get { return m_sFullName;; } }
		public long Bytes { get { return m_nBytes;; } }

		public BEFile(string sFullName)
		{
			FileInfo fi = new FileInfo(sFullName);
			InitFromParts(sFullName, fi.Length);
		}

		public BEFile(FileInfo fi)
		{
			InitFromParts(fi.FullName, fi.Length);
		}

		public void InitFromParts(string sFullName, long nBytes)
		{
			m_sFullName = sFullName;
			m_sName = Path.GetFileName(m_sFullName);
			m_nBytes = nBytes;
		}

		public BEFile(string sFullName, long nBytes)
		{
			InitFromParts(sFullName, nBytes);
		}

		public BEFile(XmlReader xr)
		{
			InitFromParts(xr.GetAttribute("name"), Int64.Parse(xr.GetAttribute("size")));
		}

		public static bool CompareFile(BEFile bef1, BEFile bef2)
		{
			if (String.Compare(bef1.m_sName, bef2.m_sName, true) != 0)
				return false;

			if (String.Compare(bef1.m_sFullName, bef2.m_sFullName, true) != 0)
				return false;

			if (bef1.m_nBytes != bef2.m_nBytes)
				return false;

			return true;
		}

	}

    // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
    // B E  D I R 
    // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	public class BEDir
    {
		string m_sName;
		string m_sFullPath;
		string m_sLabel;

		List<BEDir> m_plbed;
		List<BEFile> m_plbef;

		long m_cbDirSize;
		long m_cbDirExcl;
		DirectoryInfo m_di;

		public string Dir { get { return m_sName; } }
		public long DirSize { get { return m_cbDirSize; } }
		public long DirExcl { get { return m_cbDirExcl; } }
		public string Label { get { return m_sLabel; } }

		/* I N I T  D I R S */
		/*----------------------------------------------------------------------------
			%%Function: InitDirs
			%%Qualified: bedu.BEDir.InitDirs
			%%Contact: rlittle

		    if m_plbed is null, then we assume we haven't recursed through this
		    directory.  this isn't true if we are manually creating the directory
		    structure...we are manually recursing, so make sure we get an initialized
		    m_plbed (even if its empty)
		----------------------------------------------------------------------------*/
		public void InitDirs()
		{
			m_plbed = new List<BEDir>();
			m_plbef = new List<BEFile>();
		}

		/* I N I T  F R O M  F U L L  P A T H */
		/*----------------------------------------------------------------------------
			%%Function: InitFromFullPath
			%%Qualified: bedu.BEDir.InitFromFullPath
			%%Contact: rlittle

		----------------------------------------------------------------------------*/
		void InitFromFullPath(string sFullPath, string sLabel)
		{
			if (sFullPath[1] == ':' && sFullPath.Length == 4)
				{
				m_sName = sFullPath.Substring(0, 3);
				}
			else
				{
				m_sName = Path.GetDirectoryName(sFullPath);
				}
			m_sFullPath = sFullPath;
			m_sLabel = sLabel;
		}

		/* B  E  D I R */
		/*----------------------------------------------------------------------------
			%%Function: BEDir
			%%Qualified: bedu.BEDir.BEDir
			%%Contact: rlittle

		----------------------------------------------------------------------------*/
		public BEDir(DirectoryInfo di, string sLabel)
		{
			InitFromFullPath(di.FullName, sLabel);
			m_di = di;
		}

		/* B  E  D I R */
		/*----------------------------------------------------------------------------
			%%Function: BEDir
			%%Qualified: bedu.BEDir.BEDir
			%%Contact: rlittle

		----------------------------------------------------------------------------*/
		public BEDir(string sFullPath, string sLabel)
		{
			InitFromFullPath(sFullPath, sLabel);
		}

		/* G E T  D I R  S I Z E  F R O M  D I R E C T O R Y  I N F O */
		/*----------------------------------------------------------------------------
			%%Function: GetDirSizeFromDirectoryInfo
			%%Qualified: bedu.BEDir.GetDirSizeFromDirectoryInfo
			%%Contact: rlittle

		----------------------------------------------------------------------------*/
		public static long GetDirSizeFromDirectoryInfo(string sLabel, DirectoryInfo di, BEList belExclusions, BEList belInclusions, int cLevel, BEPref bep, IReportUsage iru, out long cBytesExcl)
		{
			long cBytes = 0;
			cBytesExcl = 0;

			iru.OpenDir(sLabel, di.FullName);

			DirectoryInfo []rgdi = null;

			// first, find any directories
			try
			{
				rgdi = di.GetDirectories();
			}
			catch {}

			if (rgdi != null)
				{
				foreach(DirectoryInfo di2 in rgdi)
					{
					long cBytesExcl2;

					cBytes += GetDirSizeFromDirectoryInfo(sLabel + @"\" + di2.Name, di2, belExclusions, belInclusions, cLevel + 1, bep, iru, out cBytesExcl2);
					cBytesExcl += cBytesExcl2;
					}
				}

			// and now add on files -- this is where we actually check exclusions/inclusions....
			FileInfo []rgfi = null;

			try 
			{
				rgfi = di.GetFiles();
			}
			catch {}

			if (rgfi != null)
				{
				foreach(FileInfo fi in rgfi)
					{
					try
						{
						string sFullName = fi.FullName;

						if (bep.DoExclusions)
							{
							if ((belExclusions != null && belExclusions.FMatch(bep.ServerName, bep.ServerShare, sFullName, BEList.BoolOp.Or))
								|| (belInclusions != null && !belInclusions.FMatch(bep.ServerName, bep.ServerShare, sFullName, BEList.BoolOp.Or)))
								{
								if (bep.Verbose)
									iru.ReportFile(0, fi.Length, sLabel, sFullName);

								cBytesExcl += fi.Length;
								continue;
								}
							}

						if (bep.Verbose)
							iru.ReportFile(fi.Length, 0, sLabel, sFullName);

						cBytes += fi.Length;
						}
				    catch (Exception e)
					    {
					    Console.WriteLine(String.Format("cannot process file {0} in directory {1}: {2}", fi.Name, di.Name, e.Message));
					    }
					}
				}

				iru.ReportDir(cBytes, cBytesExcl, sLabel, cLevel);

			iru.CloseDir();
			return cBytes;
		}

		/* P O P U L A T E  D I R */
		/*----------------------------------------------------------------------------
			%%Function: PopulateDir
			%%Qualified: bedu.BEDir.PopulateDir
			%%Contact: rlittle

		----------------------------------------------------------------------------*/
		public void PopulateDir()
		{
			m_plbed = new List<BEDir>();
			m_plbef = new List<BEFile>();

			m_cbDirSize = 0;
			m_cbDirExcl = 0;

			// build up the directory information

			DirectoryInfo []rgdi = null;

			// first, find any directories
			try
				{
				rgdi = DirInfo.GetDirectories();
			}
			catch {}

			if (rgdi != null)
				{
				foreach(DirectoryInfo di2 in rgdi)
					{
					BEDir bed = new BEDir(di2, m_sLabel + @"\" + di2.Name);

					bed.PopulateDir();
					AddDir(bed);
					}
				}
			// and now add on files -- this is where we actually check exclusions/inclusions....
			FileInfo []rgfi = null;

			try 
				{
				rgfi = DirInfo.GetFiles();
			}
			catch {}

			if (rgfi != null)
				{
				foreach(FileInfo fi in rgfi)
					{
					try
						{
						AddFile(new BEFile(fi));
						}
					catch (Exception e)
						{
						Console.WriteLine(String.Format("cannot process file {0} in directory {1}: {2}", fi.Name, DirInfo.Name, e.Message));
						}
                    }
				}
		}

		/* G E T  D I R  S I Z E */
		/*----------------------------------------------------------------------------
			%%Function: GetDirSize
			%%Qualified: bedu.BEDir.GetDirSize
			%%Contact: rlittle

		----------------------------------------------------------------------------*/
		public void ProcessDir(BEList belExclusions, BEList belInclusions, int cLevel, BEPref bep, IReportUsage iru)
		{
			if (m_plbed == null)
				PopulateDir();

			// at this point, we have populated (either because we were preloaded or we just loaded
			iru.OpenDir(m_sLabel, m_sFullPath);

			m_cbDirSize = 0;
			m_cbDirExcl = 0;

			foreach (BEDir bed in m_plbed)
				{
				bed.ProcessDir(belExclusions, belInclusions, cLevel + 1, bep, iru);
				m_cbDirSize += bed.DirSize;
				m_cbDirExcl += bed.DirExcl;
				}

			foreach (BEFile bef in m_plbef)
				{
				string sFullName = bef.FullName;
				if (bep.DoExclusions)
					{
					if ((belExclusions != null && belExclusions.FMatch(bep.ServerName, bep.ServerShare, sFullName, BEList.BoolOp.Or))
						|| (belInclusions != null && !belInclusions.FMatch(bep.ServerName, bep.ServerShare, sFullName, BEList.BoolOp.Or)))
						{
						iru.ReportFile(0, bef.Bytes, m_sLabel, sFullName);

						m_cbDirExcl += bef.Bytes;
						continue;
						}
					}

				iru.ReportFile(bef.Bytes, 0, m_sLabel, sFullName);

				m_cbDirSize += bef.Bytes;
				}

			iru.ReportDir(m_cbDirSize, m_cbDirExcl, m_sLabel, cLevel);
			iru.CloseDir();
		}

		public void AddDir(BEDir bed)
		{
			m_plbed.Add(bed);
		}

		public void AddFile(BEFile bef)
		{
			m_plbef.Add(bef);
		}

		public DirectoryInfo DirInfo{ get { if (m_di != null) return m_di; else return new DirectoryInfo(m_sName); } }

		/* C O M P A R E  D I R  D A T A */
		/*----------------------------------------------------------------------------
			%%Function: CompareDirData
			%%Qualified: bedu.BEDir.CompareDirData
			%%Contact: rlittle

		----------------------------------------------------------------------------*/
		public static bool CompareDirData(BEDir bed1, BEDir bed2)
		{
			if (String.Compare(bed1.m_sName, bed2.m_sName, true) != 0)
				return false;

			if (String.Compare(bed1.m_sFullPath, bed2.m_sFullPath, true) != 0)
				return false;

			if (String.Compare(bed1.m_sLabel, bed2.m_sLabel, true) != 0)
				return false;

			return true;
		}

		/* C O M P A R E  D I R */
		/*----------------------------------------------------------------------------
			%%Function: CompareDir
			%%Qualified: bedu.BEDir.CompareDir
			%%Contact: rlittle

		----------------------------------------------------------------------------*/
		public static bool CompareDir(BEDir bed1, BEDir bed2)
		{
			if (!CompareDirData(bed1, bed2))
				return false;

			if (bed1.m_plbed.Count != bed2.m_plbed.Count)
				return false;

			if (bed1.m_plbef.Count != bed2.m_plbef.Count)
				return false;

			for (int i = 0; i < bed1.m_plbed.Count; i++)
				{
				if (!CompareDir(bed1.m_plbed[i], bed2.m_plbed[i]))
					return false;
				}

			for (int i = 0; i < bed1.m_plbef.Count; i++)
				{
				if (!BEFile.CompareFile(bed1.m_plbef[i], bed2.m_plbef[i]))
					return false;
				}
			return true;
		}
    }
}
