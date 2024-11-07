using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PalletCheck
{
    public class InspectionReport
    {
        string OutName = "";
        List<string> AllCodes = new List<string>();
        List<string> AllBoards = new List<string>();
        List<string> AllBoardDefects = new List<string>();

        public class PalRecord
        {
            public string PalletFile;
            public Dictionary<string, int> Defects = new Dictionary<string, int>();
            public List<float> Deflections = new List<float>();
            public List<float> MissingWoodPercents = new List<float>();

            public void Defect( string Defect, string Board )
            {
                string BoardDefect = Board + "_" + Defect;
                if (!Defects.ContainsKey(BoardDefect))
                    Defects.Add(BoardDefect, 0);

                Defects[BoardDefect]++;
            }

            public PalRecord(List<string> AllDefects,List<string> AllBoards)
            {
                foreach (string Board in AllBoards)
                    foreach (string Defect in AllDefects)
                    {
                        string BoardDefect = Board + "_" + Defect;
                        Defects.Add(BoardDefect, 0);
                    }
            }
        }

        List<PalRecord> AllPallets = new List<PalRecord>();

        public InspectionReport( string OutputFilename )
        {
            OutName = OutputFilename;
            string[] Codes = PalletDefect.GetCodes();
            string[] Boards = PalletDefect.GetBoards();

            foreach (string Code in Codes) AllCodes.Add(Code);
            foreach (string Board in Boards) AllBoards.Add(Board);

            foreach(string Board in Boards)
                foreach(string Code in Codes)
                    AllBoardDefects.Add(Board+"_"+Code);    
        }

        public void AddPallet(Pallet P)
        {
            string Filename = P.Filename.Replace(',', '_');
            PalRecord PR = new PalRecord(AllCodes, AllBoards);

            int nDefects = 0;

            for (int i = 0; i < P.BList.Count; i++)
            {
                foreach (PalletDefect BD in P.BList[i].AllDefects)
                {
                    PR.Defect(BD.Code, P.BList[i].BoardName);
                    nDefects++;
                }
            }

            if (nDefects == 0)
                PR.Defect("NONE","P");

            // copy over deflections
            foreach (string BoardName in AllBoards)
                for (int i = 0; i < P.BList.Count; i++)
                    if (P.BList[i].BoardName == BoardName)
                    {
                        PR.Deflections.Add(P.BList[i].MaxDeflection);
                        PR.MissingWoodPercents.Add(P.BList[i].MissingWoodPercent);
                    }

            PR.PalletFile = Filename;

            AllPallets.Add(PR);
        }

        public void Save()
        {
            StringBuilder SB = new StringBuilder();

            SB.Append("Pallet file name,");

            // Create header line
            for (int i = 0; i < AllBoardDefects.Count; i++)
            {
                SB.Append(AllBoardDefects[i]+",");
            }
            SB.Append("TotalDefects,");

            foreach (string BoardName in AllBoards) 
                SB.Append(BoardName + "_MaxDefl,");

            foreach (string BoardName in AllBoards)
                SB.Append(BoardName + "_MWPercent,");

            SB.AppendLine("");

            //for (int i = 0; i < AllCodes.Count; i++)
            //{
            //    string DefectName = PalletDefect.CodeToName(AllCodes[i]);
            //    SB.Append(DefectName);

            //    if (i < AllCodes.Count - 1)
            //        SB.Append(", ");
            //    else
            //        SB.AppendLine(",Total,");
            //}

            for ( int i = 0; i < AllPallets.Count; i++ )
            {
                PalRecord PR = AllPallets[i];

                SB.Append(PR.PalletFile+", ");
                int total = 0;
                for (int j = 0; j < AllBoardDefects.Count; j++)
                {
                    int n = PR.Defects[AllBoardDefects[j]];
                    total += n;

                    SB.Append( n.ToString() );
                    if (j < AllBoardDefects.Count - 1)
                        SB.Append(", ");
                    else
                        SB.Append(","+total.ToString()+",");
                }

                for (int j = 0; j < AllBoards.Count; j++)
                {
                    if (j<PR.Deflections.Count)
                        SB.Append(PR.Deflections[j].ToString() + ", ");
                    else
                        SB.Append("0, ");
                }


                for (int j = 0; j < AllBoards.Count; j++)
                {
                    if (j < PR.MissingWoodPercents.Count)
                        SB.Append(PR.MissingWoodPercents[j].ToString() + ", ");
                    else
                        SB.Append("100, ");
                }

                SB.AppendLine("");
            }


            //SB.Append("Totals,");

            //for (int i = 0; i < AllCodes.Count; i++)
            //{
            //    SB.Append(string.Format("=SUM({0}2:{1}{2})", (char)('B' + i), (char)('B' + i), AllPallets.Count+1));

            //    if (i < AllCodes.Count - 1)
            //        SB.Append(", ");
            //    else
            //        SB.AppendLine("");
            //}

            System.IO.File.WriteAllText(OutName, SB.ToString());
        }
    }
}
