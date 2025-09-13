# Example Bug Issue for Testing Bug Solver Bot

## Title: Bug in calculateTotal function - incorrect handling of negative numbers

## Issue Body:

### Expected Behavior
The `calculateTotal` function should correctly calculate the sum of an array of numbers, including negative numbers.

### Actual Behavior  
The function returns incorrect results when the array contains negative numbers.

### Test Cases

```csharp
[Test]
public void CalculateTotal_WithNegativeNumbers_ShouldReturnCorrectSum()
{
    // Arrange
    var calculator = new Calculator();
    var numbers = new[] { 5, -3, 8, -2 };
    
    // Act
    var result = calculator.calculateTotal(numbers);
    
    // Assert
    Assert.AreEqual(8, result); // 5 + (-3) + 8 + (-2) = 8
}

[Test]
public void CalculateTotal_WithOnlyNegativeNumbers_ShouldReturnNegativeSum()
{
    // Arrange
    var calculator = new Calculator();
    var numbers = new[] { -1, -2, -3 };
    
    // Act
    var result = calculator.calculateTotal(numbers);
    
    // Assert  
    Assert.AreEqual(-6, result); // (-1) + (-2) + (-3) = -6
}
```

### Steps to Reproduce
1. Create a Calculator instance
2. Call calculateTotal with an array containing negative numbers
3. Observe the incorrect result

This bug is critical for our financial calculations module.