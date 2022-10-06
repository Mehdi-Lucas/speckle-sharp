#ifndef CREATE_BEAM_HPP
#define CREATE_BEAM_HPP

#include "BaseCommand.hpp"


namespace AddOnCommands {
	class CreateBeam : public BaseCommand {
	public:
		virtual GS::String							GetName() const override;
		virtual GS::ObjectState						Execute(const GS::ObjectState& parameters, GS::ProcessControl& processControl) const override;
#ifdef ServerMainVers_2600
		virtual bool								IsProcessWindowVisible() const override { return true; }
#endif
	};
}

#endif