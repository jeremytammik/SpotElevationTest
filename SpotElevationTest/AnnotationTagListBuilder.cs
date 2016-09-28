using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace SpotElevationTest
{
  class AnnotationTagListBuilder
  {
    public List<FamilySymbolWrapper> GetSpotElevationsFromFile( ExternalCommandData commandData )
    {
      //create a new list to hold the desired tag types and add them
      List<string> tagCategories = new List<string>();
      tagCategories.Add( "Spot Elevation Symbols" );

      //collect the found tags into a new list
      return GetSpotElevations( commandData, tagCategories );
    }

    private List<FamilySymbolWrapper> GetSpotElevations( ExternalCommandData commandData, List<string> tagCategories )
    {
      // get the current UI document and then the specific Revit Document
      UIDocument uiDocument = commandData.Application.ActiveUIDocument;
      Document doc = uiDocument.Document;
      //instantiate a new list to hold the tag families
      List<FamilySymbolWrapper> spotElevationTypes = new List<FamilySymbolWrapper>();

      //instantiate a new filtered element collector
      FilteredElementCollector collector = new FilteredElementCollector( doc );

      //Get the list of families in the model
      IList<Element> elements = collector.OfClass( typeof( Family ) ).ToElements();

      foreach( Element element in elements )
      {
        //get the family from the element
        Family family = (Family) element;

        //check for null and family symbol = null
        if( family != null && family.GetFamilySymbolIds() != null )
        {
          //instantiate a new family symbol id list
          List<FamilySymbol> familySymbolsList = new List<FamilySymbol>();

          //get the list of family symbol ids
          foreach( ElementId elementId in family.GetFamilySymbolIds() )
          {
            familySymbolsList.Add( (FamilySymbol) ( commandData.Application.ActiveUIDocument.Document.GetElement( elementId ) ) );
          }

          //process each family symbol
          foreach( FamilySymbol tagSymbol in familySymbolsList )
          {
            try
            {
              //check for null
              if( tagSymbol != null )
              {
                //if a set of specific annotation tag categories is desired, they will be picked up here and other tag types will be ignored
                if( tagCategories.Count > 0 )
                {
                  //check the found symbol category name against the list of desired category types
                  if( tagCategories.Contains( tagSymbol.Category.Name ) )
                  {
                    //match found so add to the list for returning
                    spotElevationTypes.Add( GetFamilySymbolWrapper( tagSymbol ) );
                  }
                }
                else //get all categories of tags
                {
                  spotElevationTypes.Add( GetFamilySymbolWrapper( tagSymbol ) );
                }
              }
            }
            catch( Exception)
            {
              //do nothing
            }
          }
        }
      }
      return spotElevationTypes;
    }

    private FamilySymbolWrapper GetFamilySymbolWrapper( FamilySymbol tagSymbol, string attachmentPointValue = null )
    {
      FamilySymbolWrapper fsw = new FamilySymbolWrapper();
      fsw.familySymbol = tagSymbol;
      fsw.categoryName = tagSymbol.Category.Name;
      fsw.id = tagSymbol.Id.ToString();
      fsw.name = tagSymbol.Name;
      fsw.familyName = tagSymbol.FamilyName;
      fsw.attachmentPosition = attachmentPointValue;
      fsw.parameters = new List<Parameter>();
      ParameterSet ps = tagSymbol.Parameters;
      foreach( Parameter p in ps )
      {
        if( !fsw.parameters.Contains( p ) )
        {
          fsw.parameters.Add( p );
        }
      }
      return fsw;
    }

  }
}
