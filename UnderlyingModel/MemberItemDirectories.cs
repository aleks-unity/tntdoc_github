namespace UnderlyingModel
{
	public struct MemberItemDirectories
	{
		public readonly string m_AssembliesDirectory;
		public readonly string m_MemfilesDirectory;
		public MemberItemDirectories(string asm, string mem)
		{
			m_AssembliesDirectory = asm;
			m_MemfilesDirectory = mem;
		}
	}
}