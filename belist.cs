using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System;
using System.IO;

namespace bedu
{
	// - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	// B  E  P A T H  P A T T E R N 
	// - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	class BEPathPattern
	{
		string m_sServer;
		Regex m_rexServer;
		bool m_fServerSimpleString;

		string m_sShare;
		Regex m_rexShare;
		bool m_fShareSimpleString;

		string[] m_rgsPattern;
		Regex[] m_rgrexPattern;
		bool [] m_rgfSimpleString;

		bool m_fMatchingLeaf;

		int m_iFirstPath;

		/* B  E  P A T H  P A T T E R N */
		/*----------------------------------------------------------------------------
			%%Function: BEPathPattern
			%%Qualified: bedu.BEPathPattern.BEPathPattern
			%%Contact: rlittle

			Constructor will break the pattern into its constituent parts
		----------------------------------------------------------------------------*/
		public BEPathPattern(string sPattern)
		{
			m_rgsPattern = sPattern.Split('\\');
			m_rgrexPattern = new Regex[m_rgsPattern.Length]; // i hope its zero filled!
			m_rgfSimpleString = new bool[m_rgsPattern.Length]; // i hope its false filled! 
			m_iFirstPath = 0;

			m_iFirstPath = IExtractServerInfo(m_rgsPattern, m_iFirstPath, out m_sServer, out m_sShare);

			m_fMatchingLeaf = false;

			if (m_iFirstPath + 1 >= m_rgsPattern.Length)
				{
				m_fMatchingLeaf = true;
				}
			else
				{
				// quick scan for all wildcards
				int i;
				for (i = m_iFirstPath; i < m_rgsPattern.Length - 1; i++)
					{
					if (String.Compare(m_rgsPattern[i], "*") != 0 
						&& String.Compare(m_rgsPattern[i], "*.*") != 0 
						&& String.Compare(m_rgsPattern[i], "**") != 0 )
						{
						break;
						}
					}
				if (i == m_rgsPattern.Length - 1)
					{
					// we got through all the directories and we never saw a non-wildcard pattern
					m_fMatchingLeaf = true;
					m_iFirstPath = m_rgsPattern.Length - 1;
					}
				}
		}

		long m_nHashCache;
		bool m_fMatchCache;
		long m_cchCache;

		/* I  E X T R A C T  S E R V E R  I N F O */
		/*----------------------------------------------------------------------------
			%%Function: IExtractServerInfo
			%%Qualified: bedu.BEPathPattern.IExtractServerInfo
			%%Contact: rlittle
		 
		    extract the server & share info (if any) from the split path.  return
		    what we should consider the "next" path element
		----------------------------------------------------------------------------*/
		static int IExtractServerInfo(string[] rgs, int iNextPath, out string sServer, out string sShare)
		{
			sServer = null;
			sShare = null;

			if (rgs[iNextPath].Length == 0 && rgs[iNextPath + 1].Length == 0)
				{
				// starts with "\\server"
				iNextPath += 2;	// skip the empty record
				sServer = rgs[iNextPath++];
				sShare = rgs[iNextPath++];
				}
			else if (rgs[iNextPath].Substring(1,1) == ":")
				{
				// this is a local host path (\\[localhost]\[drive:])
				sServer = "[localhost]";
				sShare = rgs[iNextPath].Substring(0,1) + ":";
				iNextPath++;
				}

			return iNextPath;
		}

		/* F  M A T C H  P A R T */
		/*----------------------------------------------------------------------------
			%%Function: FMatchPart
			%%Qualified: bedu.BEPathPattern.FMatchPart
			%%Contact: rlittle

			return whether or not this patten matches the given path part
		----------------------------------------------------------------------------*/
		static bool FMatchPart(string sPattern, string sPathPart, ref Regex rex, ref bool fSimpleString)
		{
			if (rex == null && !fSimpleString)
				{
				if (sPattern == null && sPathPart == null)
					return true;

				if (sPattern == null && sPathPart != null)
					return false;

				if (sPattern != null && sPathPart == null)
					return false;

				if (String.Compare(sPattern, "*") == 0)
					return true;

				if (String.Compare(sPattern, "*.*") == 0)
					return true;

				if (sPattern.IndexOf('*') == -1)
					{
					fSimpleString = true;
					}
				else
					{
					string sRex = sPattern.Replace(".", "\\.").Replace("*", ".*");
					rex = new Regex("^" + sRex + "$", RegexOptions.IgnoreCase);
					}
				}

			if (fSimpleString)
				return String.Compare(sPattern, sPathPart, true/*case insensitive*/) == 0;
			else
				return rex.Match(sPathPart).Success;
        }

		class MatchRestartStack
		{
			class MatchRestartStackItem
			{
				int m_iNextPattern;
				int m_iNextPath;

				public int NextPattern { get { return m_iNextPattern; } }
				public int NextPath { get { return m_iNextPath; } }

				public MatchRestartStackItem(int iNextPattern, int iNextPath)
				{
					m_iNextPattern = iNextPattern;
					m_iNextPath = iNextPath;
				}
			}

			List<MatchRestartStackItem> m_plmrsi; 

			public MatchRestartStack()
			{
				m_plmrsi = new List<MatchRestartStackItem>();
			}

			public void Push(int iNextPattern, int iNextPath)
			{
				m_plmrsi.Add(new MatchRestartStackItem(iNextPattern, iNextPath));
			}

			public bool Empty { get { return m_plmrsi.Count == 0; } }
			
			public void Pop(out int iNextPattern, out int iNextPath)
			{
				MatchRestartStackItem mrsi = m_plmrsi[m_plmrsi.Count - 1];

				iNextPattern = mrsi.NextPattern;
				iNextPath = mrsi.NextPath;
				m_plmrsi.RemoveAt(m_plmrsi.Count - 1);
			}
		}

		/* F  M A T C H */
		/*----------------------------------------------------------------------------
			%%Function: FMatch
			%%Qualified: bedu.BEPathPattern.FMatch
			%%Contact: rlittle

			Determine if the given path matches this pattern
		----------------------------------------------------------------------------*/
		public bool FMatch(string sServerSub, string sDirSub, string sPath)
		{
			if (sPath.Length == m_cchCache && sPath.GetHashCode() == m_nHashCache)
				return m_fMatchCache;

			string[] rgs = sPath.Split('\\');
			
			m_cchCache = sPath.Length;
			m_nHashCache = sPath.GetHashCode();

			// let's get server information
			string sServer, sShare;
			int iNext = IExtractServerInfo(rgs, 0, out sServer, out sShare);
			if (sServerSub != null)
				sServer = sServerSub;
			if (sDirSub != null)
				sShare = sDirSub;
			int iPatternNext = m_iFirstPath;

			if (!FMatchPart(m_sServer, sServer, ref m_rexServer, ref m_fServerSimpleString))
				return m_fMatchCache = false;

			if (!FMatchPart(m_sShare, sShare, ref m_rexShare, ref m_fShareSimpleString))
				return m_fMatchCache = false;

			// we have several special cases to keep track of.
			// 1) if all we have is a wildcard leaf, then we match in every  subdirectory
			//    (eg:  "*.txt"  or "\\*\*\*.txt" matches "c:\foo.txt", and "c:\bar\baz\foo.txt"
			// 	  (this does not apply if we have *any* path information - 
			// 	   "c:\*.txt" doesn't match "c:\foo\foo.txt")
			// 1.1) extension of 1) above -- if all we have is wildcard directories followed by a wildcard leaf,
			//    then we match every subdirectory...
			// 
			// 2) if we encounter the "**" pattern, we *must* match at least one directory, but
			//    after that first match, we can match any number of other directories
			// 

			int cDirsMustMatch = 0;

			bool fAnyDirMatch = (iPatternNext + 1 < m_rgsPattern.Length) && String.Compare(m_rgsPattern[iPatternNext], "**") == 0;
			if (fAnyDirMatch)
				cDirsMustMatch = 1;

			MatchRestartStack mrs = new MatchRestartStack();

			while (true)
				{
				bool fNextPatternMatches = false;

				// ok, let's try matching the current parts.  (If we're only matching the leaf then this only matters
				// when we are at the leaf)
				if (!FMatchPart(m_rgsPattern[iPatternNext], rgs[iNext], ref m_rgrexPattern[iPatternNext], ref m_rgfSimpleString[iPatternNext])
					&& (!m_fMatchingLeaf || iNext + 1 >= rgs.Length))
					{
					if (!mrs.Empty)
						{
						mrs.Pop(out iPatternNext, out iNext);
						fAnyDirMatch = true;
						continue;
						}

					return m_fMatchCache = false;
					}

				// look ahead to see if the next pattern matches too, but only if we're on "any dir"
				// and only if we've already consumed the mandatory directory match
				if (fAnyDirMatch && cDirsMustMatch == 0)
					fNextPatternMatches = FMatchPart(m_rgsPattern[iPatternNext + 1], rgs[iNext], ref m_rgrexPattern[iPatternNext + 1], ref m_rgfSimpleString[iPatternNext + 1]);

				// ok, we matched. We *always* increment the path
				iNext++;
				if (iNext >= rgs.Length)
					{
					// if we haven't consumed all of the pattern, we've failed...
					if (iPatternNext + 1 < m_rgsPattern.Length)
						{
						if (!mrs.Empty)
							{
							mrs.Pop(out iPatternNext, out iNext);
							fAnyDirMatch = true;
							continue;
							}
						return m_fMatchCache = false;
						}
					return m_fMatchCache = true;
					}

				// now, do we increment the pattern?
				// if we're matching just a leaf, then don't increment
				if (m_fMatchingLeaf)
					continue;

				// if we were matching "any dir" and we hadn't consumed a directory yet, then we
				// must consume one now
				if (cDirsMustMatch > 0)
					{
					cDirsMustMatch--;
					continue;
					}

				if (!fAnyDirMatch)
					{
#if !nosubdir
					if (iPatternNext + 1 < m_rgsPattern.Length)
#endif
						iPatternNext++;

#if nosubdir
					// if we're beyond the end and we haven't matched, then we won't ever match
					if (iPatternNext >= m_rgsPattern.Length)
						{
						if (!mrs.Empty)
							{
							mrs.Pop(out iPatternNext, out iNext);
							fAnyDirMatch = true;
							continue;
							}
						return m_fMatchCache = false;
						}
#endif

					fAnyDirMatch = (iPatternNext + 1 < m_rgsPattern.Length) && String.Compare(m_rgsPattern[iPatternNext], "**") == 0;
					if (fAnyDirMatch)
						cDirsMustMatch = 1;

					continue;
					}

				// at this point, we are matching any dir.  we have to figure out if we want to increment the
				// pattern or not

				// at this point it gets gnarly.  generally, we want to increment the pattern *if* the next part
				// of the pattern matches the current part of the path. HOWEVER, if we subsequently fail pattern
				// matching, then we have to go back and try again with a different assumption; repeating for
				// every one that we push

				// 01 2 3  4     5
				// \\*\*\**\match\*.mp3
				// c:\inner\match\nomatch\match\foo.mp3
				//   0     1     2       3     4 
				//   ^-- [3,0] Matches -> [3,1] (no lookahead)
				//         ^--  [3,1] Matches -> [3,2]
				// 				lookahead [4,1] Matches PUSH RESTART [3,2]; -> [5,2]
				//               ^-- [5,2] fails, pop -> [3,2]
				//               ^-- [3,2] Matches -> [3,3]
				//                       ^-- [3,3] Matches -> [3,4]
				// 						     lookahead [4,3] matches PUSH RESTART [3,4]; -> [5, 4]
				//                      	   ^-- [5, 4] Matches -> DONE


				// 01 2 3  4     5
				// \\*\*\**\match\*.mp3
				// c:\inner\match\match\nomatch\match\foo.mp3
				//   0     1     2     3       4     5
				//   ^-- [3,0] Matches -> [3,1] (no lookahead)
				//         ^--  [3,1] Matches -> [3,2]
				// 				lookahead [4,1] Matches PUSH RESTART [3,2]; -> [5,2]
				//               ^-- [5,2] fails, pop -> [3,2]
				//               ^-- [3,2] Matches -> [3,3]
				// 				     lookahead [4,2] Matches, PUSH RESTART [3,3] -> [5,3]
				//                       ^-- [5,3] fails, pop -> [3,3]
				// 						 ^-- [3,3] matches -> [3,4]
				// 							    ^-- [3,4] matches -> [3,5]
				// 						     		lookahead [3,4] matches PUSH RESTART [3,5]; -> [5, 5]
				//                      	   		 ^-- [5, 5] Matches -> DONE

				// 01 2 3  4     5  6      7
				// \\*\*\**\match\**\match2\*.mp3
				// c:\inner\match\match\match2\match2\nomatch\inner\match2\foo.mp3
				//   0     1     2     3      4       5      6      7     8
				//   ^-- [3,0] Matches -> [3,1] (no lookahead)
				//         ^-- [3,1] Matches -> [3,2]
				//             lookahead [4,1] Matches PUSH RESTART [3,2]; -> [5,2]
				//               ^-- [5,2] Matches -> [5,3] no lookahead
				//                     ^-- [5,3] Matches -> [5,4]
				//                         [6,3] Matches PUSH RESTART [5,4]; -> [7,4]
				//                             ^-- [7,4] fails, pop -> [5,4]
				//                             ^-- [5,4] Matches -> [5,5]
				// 								   lookahead [6,4] Matches PUSH RESTART [5,5]; -> [7,5]
				//                                    ^-- [7,5] fails, pop -> [5,5]
				//                                    ^-- [5,5] Matches -> [5,6]
				//                                           ^-- [5,6] Matches -> [5,7]
				//                                                  ^-- [5,7] Matches -> [5,8]
				//                                                      lookahead [6,7] Matches PUSH RESTART [5,8]; -> [7,8]
				//                                                          ^-- [7,8] Matches -> DONE


				if (fNextPatternMatches)
					{
					// let's remember what we would have done if we'd taken the other fork in the road...
					mrs.Push(iPatternNext, iNext);
					iPatternNext += 2; // skip both the "**" match as well as the lookahead match we just matched

					fAnyDirMatch = (iPatternNext + 1 < m_rgsPattern.Length) && String.Compare(m_rgsPattern[iPatternNext], "**") == 0;
					if (fAnyDirMatch)
						cDirsMustMatch = 1;
					continue;
					}

				// if we're about to be at the leaf of the path, we must try to match the leaf of the pattern (if we can)
				if (iNext + 1 >= rgs.Length)
					{
					iPatternNext++;
					}

				// otherwise, just continue consuming
				}

			return m_fMatchCache = false;
		}

	}
	   
    // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
    // B  E  L I S T 
    // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
    public class BEList
    {
		List<BEPathPattern> m_plpp;
		public enum BoolOp
		{
			And, 
			Or
		};

		public bool Empty { get { return m_plpp.Count == 0; } }

		public bool FMatch(string sServerSub, string sServerDir, string sPath, BoolOp bo)
		{
			foreach (BEPathPattern bepp in m_plpp)
				{
				if (bo == BoolOp.And)
					{
					if (!bepp.FMatch(sServerSub, sServerDir, sPath))
						return false;
					}
				else if (bo == BoolOp.Or)
					{
					if (bepp.FMatch(sServerSub, sServerDir, sPath))
						return true;
					}
				}

			if (bo == BoolOp.And)
				{
				// if we got here, then EVERY pattern matched
				return true;
				}
			else
				{
				// else, then EVERY pattern DID'T match
				return false;
				}
		}

		/* B  E  L I S T */
		/*----------------------------------------------------------------------------
			%%Function: BEList
			%%Qualified: bedu.BEList.BEList
			%%Contact: rlittle

		----------------------------------------------------------------------------*/
		public BEList()
		{
			m_plpp = new List<BEPathPattern>();
		}

		/* L O A D  F R O M  F I L E */
		/*----------------------------------------------------------------------------
			%%Function: LoadFromFile
			%%Qualified: bedu.BEList.LoadFromFile
			%%Contact: rlittle

		----------------------------------------------------------------------------*/
		public bool LoadFromFile(string sFile)
		{
			TextReader tr = new StreamReader(sFile);
            string sLine;

			while ((sLine = tr.ReadLine()) != null)
				{
				if (sLine.Length == 0)
					continue;

				BEPathPattern pp = new BEPathPattern(sLine);

				m_plpp.Add(pp);
				}

			return true;
		}
    }
}
