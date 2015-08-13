using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;



namespace BzAB
{

    class Program
    {
        static void Main(string[] args)
        {
            AutoBackupModel model = new AutoBackupModel();           
            model.Run();           
        }
    }
}
