using System;
using System.Collections.Generic;

#nullable disable

namespace Accio.Data
{
    public partial class CardRuling
    {
        public Guid CardRulingId { get; set; }
        public Guid CardId { get; set; }
        public Guid RulingId { get; set; }
        public Guid CreatedById { get; set; }
        public DateTime CreatedDate { get; set; }
        public Guid UpdatedById { get; set; }
        public DateTime UpdatedDate { get; set; }
        public bool Deleted { get; set; }
    }
}
