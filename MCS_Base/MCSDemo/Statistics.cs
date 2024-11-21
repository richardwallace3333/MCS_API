using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace PalletCheck
{

    public class StatEntry
    {
        public string Description { get; set; }
        public int Count1 { get; set; }
        public int Count2 { get; set; }
        public float Percent1 { get; set; }
        public float Percent2 { get; set; }

        public StatEntry( string desc )
        {
            Description = desc;
            Count1 = 0;
            Count2 = 0;
            Percent1 = 0;
            Percent2 = 0;
        }
    }

    public class Statistics
    {
        public List<StatEntry> Entries = new List<StatEntry>();

        StatEntry TotalPassE;
        StatEntry TotalFailE;
        StatEntry TotalCountE;
        StatEntry WholeE;
        StatEntry H1E;
        StatEntry H2E;
        StatEntry H3E;
        StatEntry V1E;
        StatEntry V2E;

        public Statistics()
        {
            Clear();
        }

        public void Clear()
        {
            Entries.Clear();

            TotalPassE = new StatEntry("Pallets Passed");
            TotalFailE = new StatEntry("Pallets Failed");
            TotalCountE = new StatEntry("Pallets Total");
            H1E = new StatEntry("H1");
            H2E = new StatEntry("H2");
            H3E = new StatEntry("H3");
            V1E = new StatEntry("V1");
            V2E = new StatEntry("V2");
            WholeE = new StatEntry("Other");

            Entries.Add(TotalPassE);
            Entries.Add(TotalFailE);
            Entries.Add(TotalCountE);
            Entries.Add(H1E);
            Entries.Add(H2E);
            Entries.Add(H3E);
            Entries.Add(V1E);
            Entries.Add(V2E);
            Entries.Add(WholeE);

        }
        //public enum DefectLocation
        //{
        //    Pallet,
        //    H1,
        //    H2,
        //    H3,
        //    V1,
        //    V2
        //}

        public int CountDefects(Pallet P, PalletDefect.DefectLocation Loc)
        {
            int count = 0;
            foreach (PalletDefect PD in P.AllDefects)
                if (PD.Location==Loc)
                    count++;
            return count;
        }

        public void OnNewPallet(Pallet P)
        {

            if (P.State != Pallet.InspectionState.Unprocessed)
            {
                TotalCountE.Count1 += 1;
            }

            if (P.State == Pallet.InspectionState.Pass)
            {
                TotalPassE.Count1 += 1;
            }
            
            if (P.State == Pallet.InspectionState.Fail)
            {
                TotalFailE.Count1 += 1;
            }



            //foreach (PalletDefect PD in P.AllDefects)
            {
                if (CountDefects(P, PalletDefect.DefectLocation.Pallet) > 0) WholeE.Count1 += 1;
                if (CountDefects(P, PalletDefect.DefectLocation.H1) > 0) H1E.Count1 += 1;
                if (CountDefects(P, PalletDefect.DefectLocation.H2) > 0) H2E.Count1 += 1;
                if (CountDefects(P, PalletDefect.DefectLocation.H3) > 0) H3E.Count1 += 1;
                if (CountDefects(P, PalletDefect.DefectLocation.V1) > 0) V1E.Count1 += 1;
                if (CountDefects(P, PalletDefect.DefectLocation.V2) > 0) V2E.Count1 += 1;


                //if (PD.Location == PalletDefect.DefectLocation.Pallet) WholeE.Count1 += 1;
                //if (PD.Location == PalletDefect.DefectLocation.H1) H1E.Count1 += 1;
                //if (PD.Location == PalletDefect.DefectLocation.H2) H2E.Count1 += 1;
                //if (PD.Location == PalletDefect.DefectLocation.H3) H3E.Count1 += 1;
                //if (PD.Location == PalletDefect.DefectLocation.V1) V1E.Count1 += 1;
                //if (PD.Location == PalletDefect.DefectLocation.V2) V2E.Count1 += 1;
            }

            if (TotalCountE.Count1 > 0)
            {
                TotalPassE.Percent1 = (float)Math.Round((double)(100.0 * TotalPassE.Count1 / TotalCountE.Count1), 2);
                TotalFailE.Percent1 = (float)Math.Round((double)(100.0 * TotalFailE.Count1 / TotalCountE.Count1), 2);
                TotalCountE.Percent1 = 100.0f;
            }
            if (TotalCountE.Count1 > 0)
            {
                WholeE.Percent1 = (float)Math.Round((double)(100.0 * WholeE.Count1 / TotalCountE.Count1), 2);
                H1E.Percent1 = (float)Math.Round((double)(100.0 * H1E.Count1 / TotalCountE.Count1), 2);
                H2E.Percent1 = (float)Math.Round((double)(100.0 * H2E.Count1 / TotalCountE.Count1), 2);
                H3E.Percent1 = (float)Math.Round((double)(100.0 * H3E.Count1 / TotalCountE.Count1), 2);
                V1E.Percent1 = (float)Math.Round((double)(100.0 * V1E.Count1 / TotalCountE.Count1), 2);
                V2E.Percent1 = (float)Math.Round((double)(100.0 * V2E.Count1 / TotalCountE.Count1), 2);
            }

        }
    }

    
}
