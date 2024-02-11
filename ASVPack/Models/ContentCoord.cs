using AsaSavegameToolkit;
using ASVPack.Extensions;
using SavegameToolkit;
using SavegameToolkit.Arrays;
using SavegameToolkit.Propertys;
using SavegameToolkit.Structs;
using SavegameToolkit.Types;
using SavegameToolkitAdditions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
namespace ASVPack.Models
{
    [DataContract]
    public abstract class ContentCoord
    {
        [DataMember] public float? Latitude { get; set; }
        [DataMember] public float? Longitude { get; set; }
        [DataMember] public float? X { get; set; }
        [DataMember] public float? Y { get; set; }
        [DataMember] public float? Z { get; set; }

    }
}

