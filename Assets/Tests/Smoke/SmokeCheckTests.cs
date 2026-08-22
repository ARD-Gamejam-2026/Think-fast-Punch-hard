using NUnit.Framework;

public class SmokeCheckTests
{
    [Test]
    public void Add_ReturnsSumOfBothOperands()
    {
        Assert.AreEqual(4, SmokeCheck.Add(2, 2));
    }
}
