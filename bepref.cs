using System;
using System.Collections.Generic;
using CmdLineSupport;

namespace bedu
{
	// - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	// B  E  P R E F 
	// - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	public class BEPref : ICmdLineDispatch
	{
		int m_nMaxDepth;
		bool m_fReportZeros;
        bool m_fVerbose;
		bool m_fDoExclusions;
		bool m_fRecord;
		bool m_fPlayback;
		bool m_fFast; 

		string m_sServerName;
		string m_sServerShare;
		string m_sRecordFile;
		BEList m_belExclusions;
		BEList m_belSelection;

		string m_sProgramDir;

		public BEPref(BEList belSelection, BEList belExclusions, string sProgramDir)
		{
			m_nMaxDepth = 2;
			m_fReportZeros = true;
            m_fVerbose = false;
			m_fDoExclusions = false;
			m_belExclusions = belExclusions;
			m_belSelection = belSelection;
			m_sProgramDir = sProgramDir;
		}

		public int MaxDepth { get { return m_nMaxDepth; } set { m_nMaxDepth = value; } }
		public bool ReportZeros { get { return m_fReportZeros; } set { m_fReportZeros = value; } }
        public bool Verbose { get { return m_fVerbose; } set { m_fVerbose = value; } }
		public bool DoExclusions { get { return m_fDoExclusions; } set { m_fDoExclusions = value; } }
		public bool Fast  { get { return m_fFast; ; } }

		public bool Record { get { return m_fRecord; } }
		public bool Playback { get { return m_fPlayback; } }

		public string ServerName { get { return m_sServerName; } set { m_sServerName = value; } }
		public string ServerShare { get { return m_sServerShare; } set { m_sServerShare = value; } }
		public string RecordFile { get { return m_sRecordFile; } }

		public bool FDispatchCmdLineSwitch(CmdLineSwitch cls, string sParam, object oClient, out string sError)
		{
			sError = null;
			if (cls.Switch.Length == 1)
				{
				switch (cls.Switch[0])
					{
					case 'f':
						m_fFast = true;
						break;
					case 'd':
						m_nMaxDepth = Int32.Parse(sParam);
						break;
					case 'n':
						m_sServerName = sParam;
						break;
					case 'h':
						m_sServerShare = sParam;
						break;
					case 'r':
						m_sRecordFile = sParam;
						m_fRecord = true;
						break;
					case 'p':
						m_sRecordFile = sParam;
						m_fPlayback = true;
						break;
					case '0':
					case '1':
					case '2':
					case '3':
						m_nMaxDepth = cls.Switch[0] - '0';
						break;
					case 'z':
						m_fReportZeros = true;
						break;
					case 'v':
						m_fVerbose = true;
						break;
					case 'i':
						m_belSelection.LoadFromFile(sParam);
						m_fDoExclusions = true;
						break;
					case 'X':
						m_belExclusions.LoadFromFile(sParam);
						m_fDoExclusions = true;
						break;
					case 'x':
						m_belExclusions.LoadFromFile(m_sProgramDir + @"\default_exclusions.txt");
						m_fDoExclusions = true;
						break;

					default:
						sError = String.Format("unknown arg '{0}'", cls.Switch);
						return false;
					}
				}
			return true;
		}

	}
}

