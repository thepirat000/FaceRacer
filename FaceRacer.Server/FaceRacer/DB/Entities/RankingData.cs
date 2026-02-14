using System;
using System.Collections.Generic;
using System.Text;

namespace FaceRacer.DB.Entities;
public class RankingData
{
    public Guid Id { get; set; }
    public int TrackId { get; set; }
    public string Period { get; set; }
    public DateOnly PeriodDate { get; set; }

    public DateTime InsertedDate { get; set; }
    public DateTime UpdatedDate { get; set; }

    public List<RankingDataDetails> RankingDetails { get; set; }
}