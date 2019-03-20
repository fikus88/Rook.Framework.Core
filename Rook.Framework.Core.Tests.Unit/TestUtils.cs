namespace Rook.Framework.Core.Tests.Unit
{
    public static class TestUtils
    {
        public static T GetPropertyValue<T>(object obj, string propertyName)
        {
            var propertyInfo = obj.GetType().GetProperty(propertyName);
            return propertyInfo != null ? (T)propertyInfo.GetValue(obj, null) : default(T);
        }
    }
}
