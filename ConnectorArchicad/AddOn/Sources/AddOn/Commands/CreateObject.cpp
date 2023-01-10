#include "CreateObject.hpp"
#include "ResourceIds.hpp"
#include "ObjectState.hpp"
#include "Utility.hpp"
#include "Objects/Point.hpp"
#include "RealNumber.h"
#include "DGModule.hpp"
#include "FieldNames.hpp"
#include "TypeNameTables.hpp"
#include "GSUnID.hpp"
#include "BuiltInLibrary.hpp"
#include "Folder.hpp"

namespace AddOnCommands
{

// Retrieves a valid location for creating library part
static GSErrCode GetLocation (IO::Location*& loc, bool useEmbeddedLibrary)
{
	GS::Array<API_LibraryInfo> libInfo;
	loc = nullptr;

	GSErrCode err = NoError;

	if (useEmbeddedLibrary) {
		Int32 embeddedLibraryIndex = -1;
		// get embedded library location
		if (ACAPI_Environment (APIEnv_GetLibrariesID, &libInfo, &embeddedLibraryIndex) == NoError && embeddedLibraryIndex >= 0) {
			try {
				loc = new IO::Location (libInfo[embeddedLibraryIndex].location);
			} catch (std::bad_alloc&) {
				return APIERR_MEMFULL;
			}

			if (loc != nullptr) {
				IO::Location ownFolderLoc (*loc);
				ownFolderLoc.AppendToLocal (IO::Name ("Speckle Library"));
				err = IO::fileSystem.CreateFolder (ownFolderLoc);
				if (err == NoError || err == IO::FileSystem::TargetExists)
					loc->AppendToLocal (IO::Name ("Speckle Library"));
			}
		}
	} else {
		// register our own folder and create the library part in it
		if (ACAPI_Environment (APIEnv_GetLibrariesID, &libInfo) == NoError) {
			IO::Location folderLoc;
			API_SpecFolderID specID = API_UserDocumentsFolderID;
			ACAPI_Environment (APIEnv_GetSpecFolderID, &specID, &folderLoc);
			folderLoc.AppendToLocal (IO::Name ("Speckle Library"));
			IO::Folder destFolder (folderLoc, IO::Folder::Create);
			if (destFolder.GetStatus () != NoError || !destFolder.IsWriteable ())
				return APIERR_GENERAL;

			loc = new IO::Location (folderLoc);

			for (UInt32 ii = 0; ii < libInfo.GetSize (); ii++) {
				if (folderLoc == libInfo[ii].location)
					return NoError;
			}

			try {
				API_LibraryInfo li;
				li.location = folderLoc;

				libInfo.Push (li);
			}
			catch (const GS::OutOfMemoryException&) {
				DBBREAK_STR ("Not enough memory");
				return APIERR_MEMFULL;
			}

			ACAPI_Environment (APIEnv_SetLibrariesID, &libInfo);
		}
	}

	return NoError;
}


// Sets the given LibPart as the default object
GSErrCode	SetLibPartAsDefaultObject (const API_LibPart& libPart)
{
	GSErrCode			err = NoError;
	API_Element			element;
	API_Element			mask;
	API_ElementMemo		memo;

	BNZeroMemory (&element, sizeof (API_Element));
	BNZeroMemory (&memo, sizeof (API_ElementMemo));

	element.header.type = API_ObjectID;

	API_ParamOwnerType paramOwner;
	BNZeroMemory (&paramOwner, sizeof (API_ParamOwnerType));
	paramOwner.libInd = libPart.index;
	err = ACAPI_Goodies (APIAny_OpenParametersID, &paramOwner, nullptr);
	if (err == NoError) {
		API_GetParamsType getParams;
		BNZeroMemory (&getParams, sizeof (API_GetParamsType));
		err = ACAPI_Goodies (APIAny_GetActParametersID, &getParams, nullptr);
		if (err == NoError) {
			ACAPI_DisposeAddParHdl (&memo.params);
			memo.params = getParams.params;
		}
		ACAPI_Goodies (APIAny_CloseParametersID, nullptr, nullptr);
	}

	element.object.libInd = libPart.index;
	ACAPI_ELEMENT_MASK_CLEAR (mask);
	ACAPI_ELEMENT_MASK_SET (mask, API_ObjectType, libInd);

	err = ACAPI_Element_ChangeDefaults (&element, &memo, &mask);
	if (err != NoError)
		return err;
	
	ACAPI_DisposeElemMemoHdls (&memo);

	return err;
}


// Create the library part
static GSErrCode CreateLibraryPart ()
{
	GSErrCode err = NoError;

	API_LibPart libPart;
	BNZeroMemory (&libPart, sizeof (API_LibPart));
	libPart.typeID = APILib_ObjectID;
	libPart.isTemplate = false;
	libPart.isPlaceable = true;

	const GS::UnID unID = BL::BuiltInLibraryMainGuidContainer::GetInstance ().GetUnIDWithNullRevGuid (BL::BuiltInLibPartID::ModelElementLibPartID);
	CHCopyC (unID.ToUniString ().ToCStr (), libPart.parentUnID);	// Model Element subtype

	GS::ucscpy (libPart.docu_UName, L("test"));
	
	err = GetLocation (libPart.location, true);
	if (err != NoError)
		return err;

	ACAPI_Environment (APIEnv_OverwriteLibPartID, (void *) (Int32) true, nullptr);
	err = ACAPI_LibPart_Create (&libPart);
	ACAPI_Environment (APIEnv_OverwriteLibPartID, (void *) (Int32) false, nullptr);

	if (err == NoError) {
		char buffer[1000];

		API_LibPartSection section;

		// Comment script section
		BNZeroMemory (&section, sizeof (API_LibPartSection));
		section.sectType = API_SectComText;
		ACAPI_LibPart_NewSection (&section);
		sprintf (buffer, "Speckle");
		ACAPI_LibPart_WriteSection (Strlen32 (buffer), buffer);
		ACAPI_LibPart_EndSection ();

		// Keyword section
		BNZeroMemory (&section, sizeof (API_LibPartSection));
		section.sectType = API_SectKeywords;
		ACAPI_LibPart_NewSection (&section);
		sprintf (buffer, "Speckle");
		ACAPI_LibPart_WriteSection (Strlen32 (buffer), buffer);
		ACAPI_LibPart_EndSection ();

		// Copyright section
		BNZeroMemory (&section, sizeof (API_LibPartSection));
		section.sectType = API_SectCopyright;
		ACAPI_LibPart_NewSection (&section);
		sprintf (buffer, "Speckle");	// Author
		ACAPI_LibPart_WriteSection (Strlen32 (buffer), buffer);
		ACAPI_LibPart_EndSection ();

		// Master script section
		BNZeroMemory (&section, sizeof (API_LibPartSection));
		section.sectType = API_Sect1DScript;
		ACAPI_LibPart_NewSection (&section);
		buffer[0] = '\0';
		ACAPI_LibPart_WriteSection (Strlen32 (buffer), buffer);
		ACAPI_LibPart_EndSection ();

		// 3D script section
		BNZeroMemory (&section, sizeof (API_LibPartSection));
		section.sectType = API_Sect3DScript;
		ACAPI_LibPart_NewSection (&section);
		sprintf (buffer, "MATERIAL mat%s%s", GS::EOL, GS::EOL);
		ACAPI_LibPart_WriteSection (Strlen32 (buffer), buffer);
		sprintf (buffer, "BLOCK a, b, 1%s", GS::EOL);
		ACAPI_LibPart_WriteSection (Strlen32 (buffer), buffer);
		sprintf (buffer, "ADD a * 0.5, b* 0.5, 1%s", GS::EOL);
		ACAPI_LibPart_WriteSection (Strlen32 (buffer), buffer);
		sprintf (buffer, "CYLIND zzyzx - 3, MIN (a, b) * 0.5%s", GS::EOL);
		ACAPI_LibPart_WriteSection (Strlen32 (buffer), buffer);
		sprintf (buffer, "ADDZ zzyzx - 3%s", GS::EOL);
		ACAPI_LibPart_WriteSection (Strlen32 (buffer), buffer);
		sprintf (buffer, "CONE 2, MIN (a, b) * 0.5, 0.0, 90, 90%s", GS::EOL);
		ACAPI_LibPart_WriteSection (Strlen32 (buffer), buffer);
		ACAPI_LibPart_EndSection ();

		// 2D script section
		BNZeroMemory (&section, sizeof (API_LibPartSection));
		section.sectType = API_Sect2DScript;
		ACAPI_LibPart_NewSection (&section);
		sprintf (buffer, "PROJECT2 3, 270, 2%s", GS::EOL);
		ACAPI_LibPart_WriteSection (Strlen32 (buffer), buffer);
		ACAPI_LibPart_EndSection ();

		// Parameter script section
		BNZeroMemory (&section, sizeof (API_LibPartSection));
		section.sectType = API_SectVLScript;
		ACAPI_LibPart_NewSection (&section);
		sprintf (buffer, "VALUES \"zzyzx\" RANGE [6,]%s", GS::EOL);
		ACAPI_LibPart_WriteSection (Strlen32 (buffer), buffer);
		ACAPI_LibPart_EndSection ();

		// Parameters section
		BNZeroMemory (&section, sizeof (API_LibPartSection));
		section.sectType = API_SectParamDef;

		short nPars = 4;
		API_AddParType** addPars = reinterpret_cast<API_AddParType**>(BMAllocateHandle (nPars * sizeof (API_AddParType), ALLOCATE_CLEAR, 0));
		if (addPars != nullptr) {
			API_AddParType* pAddPar = &(*addPars)[0];
			pAddPar->typeID = APIParT_Mater;
			pAddPar->typeMod = 0;
			CHTruncate ("mat", pAddPar->name, sizeof (pAddPar->name));
			GS::ucscpy (pAddPar->uDescname, L("Material"));
			pAddPar->value.real = 1;

			pAddPar = &(*addPars)[1];
			pAddPar->typeID = APIParT_Length;
			pAddPar->typeMod = 0;
			CHTruncate ("len", pAddPar->name, sizeof (pAddPar->name));
			GS::ucscpy (pAddPar->uDescname, L("Length"));
			pAddPar->value.real = 2.5;

			pAddPar = &(*addPars)[2];
			pAddPar->typeID = APIParT_CString;
			pAddPar->typeMod = 0;
			CHTruncate ("myStr", pAddPar->name, sizeof (pAddPar->name));
			GS::ucscpy (pAddPar->uDescname, L("String parameter"));
			GS::ucscpy (pAddPar->value.uStr, L("this is a string"));

			pAddPar = &(*addPars)[3];
			pAddPar->typeID = APIParT_RealNum;
			pAddPar->typeMod = API_ParArray;
			pAddPar->dim1 = 3;
			pAddPar->dim2 = 4;
			CHTruncate ("matrix", pAddPar->name, sizeof (pAddPar->name));
			GS::ucscpy (pAddPar->uDescname, L("Array parameter with real numbers"));
			pAddPar->value.array = BMAllocateHandle (pAddPar->dim1 * pAddPar->dim2 * sizeof (double), ALLOCATE_CLEAR, 0);
			double** arrHdl = reinterpret_cast<double**>(pAddPar->value.array);
			for (Int32 k = 0; k < pAddPar->dim1; k++)
				for (Int32 j = 0; j < pAddPar->dim2; j++)
					(*arrHdl)[k * pAddPar->dim2 + j] = (k == j ? 1.1 : 0.0);

			double aa = 1.0;
			double bb = 1.0;
			GSHandle sectionHdl = nullptr;
			ACAPI_LibPart_GetSect_ParamDef (&libPart, addPars, &aa, &bb, nullptr, &sectionHdl);

			API_LibPartDetails details;
			BNZeroMemory (&details, sizeof (API_LibPartDetails));
			details.object.autoHotspot = false;
			ACAPI_LibPart_SetDetails_ParamDef (&libPart, sectionHdl, &details);

			ACAPI_LibPart_AddSection (&section, sectionHdl, nullptr);

			BMKillHandle (reinterpret_cast<GSHandle*>(&arrHdl));
			BMKillHandle (reinterpret_cast<GSHandle*>(&addPars));
			BMKillHandle (&sectionHdl);
		} else {
			err = APIERR_MEMFULL;
		}

		if (libPart.location != nullptr) {
			delete libPart.location;
			libPart.location = nullptr;
		}

		// Save the constructed library part
		if (err == NoError)
			err = ACAPI_LibPart_Save (&libPart);

		if (libPart.location != nullptr) {
			delete libPart.location;
			libPart.location = nullptr;
		}
	}

	if (err == NoError) {
		err = SetLibPartAsDefaultObject (libPart);
	}

	return NoError;
}

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
	
	CreateLibraryPart ();
	
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

