var relayToPinMapping = new Array();

relayToPinMapping[1] = 11;
relayToPinMapping[2] = 12;
relayToPinMapping[3] = 13;
relayToPinMapping[4] = 15;

exports.getPinByRelayId = function (relayid) {
    return relayToPinMapping[relayid];
};