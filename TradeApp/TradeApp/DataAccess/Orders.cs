//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace TradeApp.DataAccess
{
    using System;
    using System.Collections.Generic;
    
    public partial class Orders
    {
        public int Id { get; set; }
        public string Pair { get; set; }
        public string Type { get; set; }
        public string OrderType { get; set; }
        public Nullable<decimal> Price { get; set; }
        public Nullable<decimal> Price2 { get; set; }
        public decimal Volume { get; set; }
        public string Leverage { get; set; }
        public string Position { get; set; }
        public string OFlags { get; set; }
        public string Starttm { get; set; }
        public string Expiretm { get; set; }
        public string UserRef { get; set; }
        public bool Validate { get; set; }
        public string TxId { get; set; }
        public string Status { get; set; }
        public string Reason { get; set; }
        public string OpenTime { get; set; }
        public string CloseTime { get; set; }
        public Nullable<decimal> VolumeExecuted { get; set; }
        public Nullable<decimal> Cost { get; set; }
        public Nullable<decimal> Fee { get; set; }
        public Nullable<decimal> AveragePrice { get; set; }
        public Nullable<decimal> StopPrice { get; set; }
        public Nullable<decimal> LimitPrice { get; set; }
        public string Info { get; set; }
        public string Trades { get; set; }
        public Nullable<int> CandleStickId { get; set; }
        public Nullable<int> ConfirmationId { get; set; }
        public string ConditionalCloseString { get; set; }
        public Nullable<System.DateTimeOffset> CreateDate { get; set; }
    
        public virtual CandleSticks CandleSticks { get; set; }
        public virtual Confirmations Confirmations { get; set; }
    }
}