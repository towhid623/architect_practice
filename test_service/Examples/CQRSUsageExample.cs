// Example: How to use CQRS commands and queries in the test_service

// In a controller or service:
// 1. Inject the handlers via constructor
// 2. Create command/query objects
// 3. Call HandleAsync() method
// 4. Handle the Result

/*
using SharedKernel.Commands.Medicine;
using SharedKernel.Queries.Medicine;
using SharedKernel.CQRS;

// Example 1: Creating a medicine
var command = new CreateMedicineCommand(
    Name: "Aspirin",
    GenericName: "Acetylsalicylic Acid",
    Manufacturer: "Bayer",
    Description: "Pain reliever and anti-inflammatory",
    DosageForm: "Tablet",
    Strength: "500mg",
    Price: 9.99m,
    StockQuantity: 100,
    RequiresPrescription: false,
    ExpiryDate: DateTime.UtcNow.AddYears(2),
    Category: "Pain Relief",
    SideEffects: new List<string> { "Stomach upset", "Dizziness" },
    StorageInstructions: "Store at room temperature"
);

var result = await createHandler.HandleAsync(command);
if (result.IsSuccess)
{
    var medicine = result.Value;
    Console.WriteLine($"Created medicine: {medicine.Name} with ID: {medicine.Id}");
}
else
{
    Console.WriteLine($"Error: {result.Error}");
}

// Example 2: Getting all medicines
var query = new GetAllMedicinesQuery();
var medicines = await getAllHandler.HandleAsync(query);
if (medicines.IsSuccess)
{
    foreach (var med in medicines.Value)
    {
        Console.WriteLine($"{med.Name} - ${med.Price}");
    }
}

// Example 3: Searching medicines
var searchQuery = new SearchMedicinesQuery("aspirin");
var searchResult = await searchHandler.HandleAsync(searchQuery);

// Example 4: Updating a medicine
var updateCommand = new UpdateMedicineCommand(
    Id: "someId",
  Name: "Aspirin Updated",
    GenericName: null,
    Manufacturer: null,
    Description: null,
    DosageForm: null,
    Strength: null,
    Price: 10.99m,
  StockQuantity: 150,
    RequiresPrescription: null,
    IsAvailable: true,
    ExpiryDate: null,
    Category: null,
    SideEffects: null,
    StorageInstructions: null
);

var updateResult = await updateHandler.HandleAsync(updateCommand);

// Example 5: Deleting a medicine
var deleteCommand = new DeleteMedicineCommand("someId");
var deleteResult = await deleteHandler.HandleAsync(deleteCommand);
if (deleteResult.IsSuccess)
{
    Console.WriteLine("Medicine deleted successfully");
}
*/
