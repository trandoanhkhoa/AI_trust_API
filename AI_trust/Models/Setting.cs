using System;
using System.Collections.Generic;

namespace AI_trust.Models;

public partial class Setting
{
    public int Id { get; set; }

    public bool? Time { get; set; }

    public bool? Shufflequestion { get; set; }

    public int? Timelimit { get; set; }
}
