using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Net;

namespace MinecraftSpy;

[Table("subscription")]
public class Subscription
{
    [Key]
    [Column("id")]
    public Guid ID { get; set; } = Guid.NewGuid();

    [Column("channel_id")]
    public required ulong ChannelID { get; set; }

    [Column("message_id")]
    public required ulong MessageID { get; set; }

    [Column("server_address")]
    public required string ServerAddress { get; set; }

    [NotMapped]
    public IPAddress[]? ResolvedServerAddresses { get; set; }

    [Column("server_port")]
    public required short ServerPort { get; set; }

    [Column("created_by")]
    public required ulong CreatedBy { get; set; }

    [Column("created_at")]
    public required DateTime CreatedAt { get; set; }
}
