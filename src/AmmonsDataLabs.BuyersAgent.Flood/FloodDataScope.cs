namespace AmmonsDataLabs.BuyersAgent.Flood;

/// <summary>
/// Indicates the scope of data used for a flood lookup result.
/// </summary>
public enum FloodDataScope
{
    /// <summary>
    /// Scope could not be determined.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Data is specific to the parcel (lotplan).
    /// </summary>
    Parcel,

    /// <summary>
    /// Data is aggregated at the plan level (used as fallback).
    /// </summary>
    PlanFallback
}