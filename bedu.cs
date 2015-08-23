using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using CmdLineSupport;
using System.Xml;

namespace bedu
{
	// - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	// I  R E P O R T  U S A G E 
	// - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
	public interface IReportUsage
	{
		void ReportFile(long cBytes, long cBytesExcl, string sParent, string sFile);
		void ReportDir(long cBytes, long cBytesExcl, string sDir, int cLevel);
		void OpenDir(string sParent, string sFullName);
		void CloseDir();
        void WritePrologue();
        void WriteEpilogue();
		void Flush();
	}

    class bedu
    {
        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        // C O N S O L E  R E P O R T  U S A G E 
        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        public class ConsoleReportUsage : IReportUsage
        {
            BEPref m_bep;

            /* C O N S O L E  R E P O R T  U S A G E */
            /*----------------------------------------------------------------------------
            	%%Function: ConsoleReportUsage
            	%%Qualified: bedu.bedu:ConsoleReportUsage.ConsoleReportUsage
            	%%Contact: rlittle

            ----------------------------------------------------------------------------*/
            public ConsoleReportUsage(BEPref bep)
            {
                m_bep = bep;
            }

            /* R E P O R T  F I L E */
            /*----------------------------------------------------------------------------
            	%%Function: ReportFile
            	%%Qualified: bedu.bedu:ConsoleReportUsage.ReportFile
            	%%Contact: rlittle

            ----------------------------------------------------------------------------*/
            public void ReportFile(long cBytes, long cBytesExcl, string sParent, string sFile)
            {
                if (m_bep.Verbose)
                    {
                    if (m_bep.DoExclusions)
                        Console.WriteLine(String.Format("{0,15} {1,15}({2})", cBytes.ToString("#,0"), cBytesExcl.ToString("#,0"), sFile));
                    else
                        Console.WriteLine(String.Format("{0,15} ({1})", cBytes.ToString("#,0"), sFile));
                    }
            }

            /* R E P O R T  D I R */
            /*----------------------------------------------------------------------------
            	%%Function: ReportDir
            	%%Qualified: bedu.bedu:ConsoleReportUsage.ReportDir
            	%%Contact: rlittle

            ----------------------------------------------------------------------------*/
            public void ReportDir(long cBytes, long cBytesExcl, string sDir, int cLevel)
            {
                if (m_bep.MaxDepth > cLevel || m_bep.Verbose)
                    {
       				if (m_bep.ReportZeros || cBytes != 0 || cBytesExcl != 0)
                        {
                        if (m_bep.DoExclusions)
                            Console.WriteLine(String.Format("{0,15} {1,15} {2}", cBytes.ToString("#,0"), cBytesExcl.ToString("#,0"), sDir));
                        else
                            Console.WriteLine(String.Format("{0,15} {1}", cBytes.ToString("#,0"), sDir));
                        }
                    }
            }

            /* O P E N  D I R */
            /*----------------------------------------------------------------------------
            	%%Function: OpenDir
            	%%Qualified: bedu.bedu:ConsoleReportUsage.OpenDir
            	%%Contact: rlittle

            ----------------------------------------------------------------------------*/
            public void OpenDir(string sParent, string sFullName) { }

            /* C L O S E  D I R */
            /*----------------------------------------------------------------------------
            	%%Function: CloseDir
            	%%Qualified: bedu.bedu:ConsoleReportUsage.CloseDir
            	%%Contact: rlittle

            ----------------------------------------------------------------------------*/
            public void CloseDir() { }

            public void WritePrologue() { }
            public void WriteEpilogue() { }
			public void Flush() { }
        }

        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        // R E C O R D  R E P O R T  U S A G E 
        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        public class RecordReportUsage : IReportUsage
        {
            BEPref m_bep;
            TextWriter m_tw;
            string m_sServer;
            string m_sShare;

            static string Xmlify(string s)
            {
                return s.Replace("&", "&amp;").Replace("'", "&apos;");
            }

            /* R E C O R D  R E P O R T  U S A G E */
            /*----------------------------------------------------------------------------
            	%%Function: RecordReportUsage
            	%%Qualified: bedu.bedu:RecordReportUsage.RecordReportUsage
            	%%Contact: rlittle

            ----------------------------------------------------------------------------*/
            public RecordReportUsage(BEPref bep, string sServer, string sShare)
            {
                m_bep = bep;
                if (sServer == null)
                    sServer = "";
                m_sServer = Xmlify(sServer);
                m_sShare = Xmlify(sShare);
                m_tw = new StreamWriter(bep.RecordFile);
            }

            /* F L U S H */
            /*----------------------------------------------------------------------------
            	%%Function: Flush
            	%%Qualified: bedu.bedu:RecordReportUsage.Flush
            	%%Contact: rlittle

            ----------------------------------------------------------------------------*/
            public void Flush()
            {
                m_tw.Flush();
            }

            /* R E P O R T  F I L E */
            /*----------------------------------------------------------------------------
            	%%Function: ReportFile
            	%%Qualified: bedu.bedu:RecordReportUsage.ReportFile
            	%%Contact: rlittle

            ----------------------------------------------------------------------------*/
            public void ReportFile(long cBytes, long cBytesExcl, string sParent, string sFile)
            {
                sFile = Xmlify(sFile);
                sParent = Xmlify(sParent);
                m_tw.WriteLine(String.Format("<file size='{0}' excl='{1}' name='{2}'/>", cBytes, cBytesExcl, sFile));
            }

            /* R E P O R T  D I R */
            /*----------------------------------------------------------------------------
            	%%Function: ReportDir
            	%%Qualified: bedu.bedu:RecordReportUsage.ReportDir
            	%%Contact: rlittle

            ----------------------------------------------------------------------------*/
            public void ReportDir(long cBytes, long cBytesExcl, string sDir, int cLevel)
            {
                sDir = Xmlify(sDir);
                m_tw.WriteLine(String.Format("<dirSum size='{0}' excl='{1}' name='{2}'/>", cBytes, cBytesExcl, sDir));
            }

            /* O P E N  D I R */
            /*----------------------------------------------------------------------------
            	%%Function: OpenDir
            	%%Qualified: bedu.bedu:RecordReportUsage.OpenDir
            	%%Contact: rlittle

            ----------------------------------------------------------------------------*/
            public void OpenDir(string sParent, string sFullName) 
            { 
                sFullName = Xmlify(sFullName);
                sParent = Xmlify(sParent);
                m_tw.WriteLine(String.Format("<dir name='{0}'>", sFullName));
            }

            /* C L O S E  D I R */
            /*----------------------------------------------------------------------------
            	%%Function: CloseDir
            	%%Qualified: bedu.bedu:RecordReportUsage.CloseDir
            	%%Contact: rlittle

            ----------------------------------------------------------------------------*/
            public void CloseDir() 
            { 
                m_tw.WriteLine("</dir>");
            }

            /* W R I T E  P R O L O G U E */
            /*----------------------------------------------------------------------------
            	%%Function: WritePrologue
            	%%Qualified: bedu.bedu:RecordReportUsage.WritePrologue
            	%%Contact: rlittle

            ----------------------------------------------------------------------------*/
            public void WritePrologue()
            {
                m_tw.WriteLine(String.Format("<root serverPath='{0}' drive='{1}'>", m_sServer, m_sShare));
            }

            /* W R I T E  E P I L O G U E */
            /*----------------------------------------------------------------------------
            	%%Function: WriteEpilogue
            	%%Qualified: bedu.bedu:RecordReportUsage.WriteEpilogue
            	%%Contact: rlittle

            ----------------------------------------------------------------------------*/
            public void WriteEpilogue()
            {
                m_tw.WriteLine("</root>");
            }
        }


        static void UnitTest()
        {
            CmdLine.UnitTest();
        }

		enum XmlReadState
		{
			Initial,
			Root,
			DirOrFile,
			Done
		};

		/* B E D  R E A D  F R O M  X M L */
		/*----------------------------------------------------------------------------
			%%Function: BedReadFromXml
			%%Qualified: bedu.bedu.BedReadFromXml
			%%Contact: rlittle

		----------------------------------------------------------------------------*/
		public static BEDir BedFromXml(XmlReader xr, BEPref bep)
		{
			List<BEDir> plbedStack = new List<BEDir>();
			BEDir bed = null;
			BEDir bedRoot = null;
			string sLabel = ".";

			XmlReadState xrs = XmlReadState.Initial;
			while (true)
				{
                xr.MoveToContent();
				if (xrs == XmlReadState.Initial)
					{
					if (xr.LocalName != "root")
						throw new Exception(String.Format("illegal element in initial state: {0}", xr.LocalName));

                    bep.ServerName = xr.GetAttribute("serverPath");
                    bep.ServerShare  = xr.GetAttribute("drive");
                    xr.ReadStartElement();
                    xr.MoveToContent();
					xrs = XmlReadState.Root;
					}

				if (xrs == XmlReadState.DirOrFile || xrs == XmlReadState.Root)
					{
					string sFullName = xr.GetAttribute("name");

					if (xr.IsStartElement())
						{
						if (xr.LocalName == "dir")
							{
							if (xrs == XmlReadState.DirOrFile)
                                {
                                // here we cheat.  we know that we're beyond the root, and we know that the
                                // directory WILL NOT have a trailing "\" which means the path code will be
                                // confused and will think the directory name at the end is a filename...
								sLabel = sLabel + @"\" + Path.GetFileName(sFullName);
                                }

                            if (sFullName[sFullName.Length - 1] != '\\')
                                sFullName += "\\";

							BEDir bedNew = new BEDir(sFullName, sLabel);
                            bedNew.InitDirs();

							if (bedRoot == null)
								{
								bed = bedRoot = bedNew;
                                if (!xr.IsEmptyElement)
                                    plbedStack.Add(null);
								}
							else
								{
								bed.AddDir(bedNew);
                                if (!xr.IsEmptyElement)
                                    {
                                    plbedStack.Add(bed);
                                    bed = bedNew;
                                    }
								}
							}
						else if (xr.LocalName == "file")
							{
							if (xrs == XmlReadState.Root)
								throw new Exception(String.Format("illegal element in root state state: {0}", xr.LocalName));

							BEFile bef = new BEFile(xr);

							bed.AddFile(bef);
							}

						xr.ReadStartElement();
                        xrs = XmlReadState.DirOrFile;
						}
					else
						{
						// we're at an end element
						if (xrs == XmlReadState.Root)
							throw new Exception(String.Format("found close element when looking for root dir: {0}", xr.LocalName));

						if (xr.LocalName == "dir")
							{
							// pop the directory
							bed = plbedStack[plbedStack.Count - 1];
                            if (bed != null)
                                sLabel = bed.Label;
							plbedStack.RemoveAt(plbedStack.Count - 1);
							xr.ReadEndElement();
							xrs = XmlReadState.DirOrFile;
							}
						else if (xr.LocalName == "root")
							{
							// we're done
							if (plbedStack.Count != 0)
								throw new Exception(String.Format("encountered </root> with dirs on the stack: {0}", plbedStack.Count));
							xr.ReadEndElement();
							xrs = XmlReadState.Done;
							break;
							}
						else
							{
							throw new Exception(String.Format("encountered unknown close element: {0}", xr.LocalName));
							}
						}
					continue;
					}
				}
			// we should be done...
			return bedRoot;
		}

        static void Main(string[] args)
        {
            UnitTest();
			string sError;

            CmdLineConfig clcfg = new CmdLineConfig(new CmdLineSwitch[]
                {
                    new CmdLineSwitch("r", false, false, "record to a file", null, null),
					new CmdLineSwitch("f", true, false, "fast mode", null, null),
                    new CmdLineSwitch("p", false, false, "playback from a file", null, null),
                    new CmdLineSwitch("n", false, false, "name this server (for later playback)", null, null),
                    new CmdLineSwitch("h", false, false, "name this server share (for later playback)", null, null),
                    new CmdLineSwitch("v", true, false, "verbose - show each file", null, null),
					new CmdLineSwitch("z", true, false, "zeros - show directories that are 0 size", null, null),
					new CmdLineSwitch("d", false, false, "depth - max depth to show", null, null),
					new CmdLineSwitch("0", true, false, "depth 0 - alias for -d 0", null, null),
					new CmdLineSwitch("1", true, false, "depth 0 - alias for -d 1", null, null),
					new CmdLineSwitch("2", true, false, "depth 0 - alias for -d 2", null, null),
					new CmdLineSwitch("3", true, false, "depth 0 - alias for -d 3", null, null),
					new CmdLineSwitch("x", true, false, "exclusions - use the global exclusion list", null, null),
                    new CmdLineSwitch("X", false, false, "eXclusions - load more exclusions from the named file", null, null),
                    new CmdLineSwitch("i", false, false, "inclusions - this is the selection list to match against", null, null)
                } );
            BEList belExclusions = new BEList();
            BEList belSelection = new BEList();
			BEDir bedProgram = new BEDir(Assembly.GetExecutingAssembly().Location, "");
            BEPref bep = new BEPref(belSelection, belExclusions, bedProgram.Dir);

            bep.MaxDepth=2;

            CmdLine cl = new CmdLine(clcfg);

			if (!cl.FParse(args, bep, null, out sError))
				{
				Console.WriteLine(sError);
				return;
				}

            if (belSelection.Empty)
                belSelection = null;

			BEDir bedRoot = new BEDir(Environment.CurrentDirectory + @"\", ".");	// make sure we append a "/" because otherwise we won't know its a directory

            long cBytesExcl;

//            bedRoot.GetDirSize(".", bedRoot.DirInfo, belExclusions, null, 0, bep, out cBytesExcl);
            // are we recording, playing back, or what?
			IReportUsage iru;
			if (bep.Fast)
				{
				iru = new ConsoleReportUsage(bep);
				BEDir.GetDirSizeFromDirectoryInfo(".", bedRoot.DirInfo, belExclusions, belSelection, 0, bep, iru, out cBytesExcl);
				}
            else
                {
                if (bep.Playback)
                    {
					bedRoot = BedFromXml(XmlReader.Create(new StreamReader(bep.RecordFile)), bep);
                    // load the file here...
                    }

                if (bep.Record)
                    {
                    iru = new RecordReportUsage(bep, bep.ServerName, bep.ServerShare);
                    }
                else
                    {
                    iru = new ConsoleReportUsage(bep);
                    }

                iru.WritePrologue();
                bedRoot.ProcessDir(belExclusions, belSelection, 0, bep, iru);
                iru.WriteEpilogue();
				iru.Flush();
                }
        }

    }
}
