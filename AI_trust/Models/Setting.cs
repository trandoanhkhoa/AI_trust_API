using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace AI_trust.Models;

[Table("settings")]
public partial class Setting
{
    public int Id { get; set; }

    public bool? Time { get; set; }

    public bool? Shufflequestion { get; set; }

    public int? Timelimit { get; set; }
}
