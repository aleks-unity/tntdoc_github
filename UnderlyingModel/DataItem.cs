using System;

namespace UnderlyingModel
{
    public interface IDataItem
    {
        String ItemName();
        String ItemType();
        
        AssemblyDataItem GetAssemblyDataItem();
        DocumentDataItem GetDocumentDataItem();
    	void SetDocumentDataItem(DocumentDataItem other);
    }
}
