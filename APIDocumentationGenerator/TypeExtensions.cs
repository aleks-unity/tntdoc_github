using Mono.Cecil;

static internal class TypeExtensions
{
	public static bool IsVoid(this TypeReference propertyType)
	{
		return propertyType == propertyType.Module.TypeSystem.Void;
	}

	public static bool IsBoolean(this TypeReference propertyType)
	{
		return propertyType == propertyType.Module.TypeSystem.Boolean;
	}

	public static bool IsPublic(this PropertyDefinition prop)
	{
		if (prop.SetMethod != null)
			if (prop.SetMethod.IsPublic)
				return true;
		if (prop.GetMethod != null)
			if (prop.GetMethod.IsPublic)
				return true;
		return false;
	}
}
