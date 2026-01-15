using System;
using System.Collections.Generic;

namespace AI_trust.Models;

public partial class Useranswer
{
    public int Id { get; set; }

    public int? Userid { get; set; }

    public int? Questionid { get; set; }

    public string? Useranswer1 { get; set; }

    public int? Trytimes { get; set; }

    public DateTime? Startedat { get; set; }

    public DateTime? Submittedat { get; set; }

    public bool? Usetime { get; set; }

    public virtual Question? Question { get; set; }

    public virtual User? User { get; set; }
}
