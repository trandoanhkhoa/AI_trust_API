using System;
using System.Collections.Generic;

namespace AI_trust.Models;

public partial class Responseai
{
    public int Id { get; set; }

    public int? Userid { get; set; }

    public string? Questionuser { get; set; }

    public string? Answerai { get; set; }

    public DateTime? Time { get; set; }

    public int? Questionid { get; set; }

    public virtual Question? Question { get; set; }

    public virtual User? User { get; set; }
}
