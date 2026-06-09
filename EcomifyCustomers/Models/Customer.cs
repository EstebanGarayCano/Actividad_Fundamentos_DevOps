using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EcomifyCustomers.Models;

[Table("ecommify_customers")]
public class Customer
{
    [Key]
    [Column("customer_id")]
    public string CustomerId { get; set; } = string.Empty;

    [Column("customer_unique_id")]
    public string? CustomerUniqueId { get; set; }

    // address_type no se puede mapear como propiedad EF Core — se maneja via raw SQL
    [NotMapped]
    public CustomerAddress? CustomerAddress { get; set; }
}