using RentalPortal.Web.Domain;
using System;
using System.Collections.Generic;
using System.Text;

namespace RentalPortal.Tests;

public class UnitTypeTests
{
    /// <summary>
    /// An active type can be chosen in every case: for a new unit, or for an existing unit that
    /// currently has a different type.
    /// </summary>
    [Fact]
    public void ActiveType_IsSelectableForAnyUnit()
    {
        // Arrange
        var type = new UnitType { Id = 1, Name = "Studio", IsActive = true };

        // Act & Assert
        Assert.True(type.IsSelectableFor(null)); // new unit
        Assert.True(type.IsSelectableFor(2));    // existing unit switching from another type
    }

    /// <summary>
    /// An inactive type can't be chosen for a new unit or for a unit switching to it, but a unit that
    /// already has it can keep it, so editing that unit's other fields still saves.
    /// </summary>
    [Fact]
    public void InactiveType_IsOnlySelectableForAUnitAlreadyUsingIt()
    {
        // Arrange: Townhouse (Id 4 in the seed data) marked inactive for this test.
        var type = new UnitType { Id = 4, Name = "Townhouse", IsActive = false };

        // Act & Assert
        Assert.False(type.IsSelectableFor(null)); // new unit can't pick an inactive type
        Assert.False(type.IsSelectableFor(2));    // existing unit can't switch to an inactive type
        Assert.True(type.IsSelectableFor(4));     // unit already using it keeps it
    }
}
