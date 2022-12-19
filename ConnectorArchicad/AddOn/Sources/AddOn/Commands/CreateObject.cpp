#include "CreateObject.hpp"
#include "ResourceIds.hpp"
#include "ObjectState.hpp"
#include "Utility.hpp"
#include "Objects/Point.hpp"
#include "RealNumber.h"
#include "DGModule.hpp"
#include "FieldNames.hpp"
#include "TypeNameTables.hpp"

namespace AddOnCommands
{
static GSErrCode CreateNewObject (API_Element& object, API_ElementMemo* memo)
{
	return ACAPI_Element_Create (&object, memo);
}


static GSErrCode ModifyExistingObject (API_Element& object, API_Element& mask, API_ElementMemo* memo)
{
	return ACAPI_Element_Change (&object, &mask, memo, 0, true);
}


static GSErrCode GetObjectFromObjectState (const GS::ObjectState& os, API_Element& element, API_Element& objectMask, API_ElementMemo* memo)
{
	GSErrCode err = NoError;

	GS::UniString guidString;
	os.Get (ApplicationIdFieldName, guidString);
	element.header.guid = APIGuidFromString (guidString.ToCStr ());
#ifdef ServerMainVers_2600
	element.header.type.typeID = API_ObjectID;
#else
	element.header.typeID = API_ObjectID;
#endif
	err = Utility::GetBaseElementData (element, memo);
	if (err != NoError)
		return err;

	Objects::Point3D pos;
	if (os.Contains (Object::pos))
		os.Get (Object::pos, pos);
	element.object.pos = pos.ToAPI_Coord ();
	ACAPI_ELEMENT_MASK_SET (objectMask, API_ObjectType, pos);

	return NoError;
}

GS::String CreateObject::GetName () const
{
	return CreateObjectCommandName;
}

GS::ObjectState CreateObject::Execute (const GS::ObjectState& parameters, GS::ProcessControl& /*processControl*/) const
{
	GS::ObjectState result;

	GS::Array<GS::ObjectState> objects;
	parameters.Get (ObjectsFieldName, objects);

	const auto& listAdder = result.AddList<GS::UniString> (ApplicationIdsFieldName);

	ACAPI_CallUndoableCommand ("CreateSpeckleObject", [&] () -> GSErrCode {
		for (const GS::ObjectState& objectOs : objects) {
			API_Element object{};
			API_Element objectMask{};
			API_ElementMemo memo{};
			
			GSErrCode err = GetObjectFromObjectState (objectOs, object, objectMask, &memo);
			if (err != NoError)
				continue;

			bool objectExists = Utility::ElementExists (object.header.guid);
			if (objectExists) {
				err = ModifyExistingObject (object, objectMask, &memo);
			} else {
				err = CreateNewObject (object, &memo);
			}

			if (err == NoError) {
				GS::UniString elemId = APIGuidToString (object.header.guid);
				listAdder (elemId);
			}

			ACAPI_DisposeElemMemoHdls (&memo);
		}
		return NoError;
		});

	return result;
}

}

