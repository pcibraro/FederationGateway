import { SET_RPS, RP_FETCHED, RP_UPDATED, RP_DELETED, ADD_RP } from '../actions';

export default function relyingParties(state = [], action = {}) {
  switch(action.type) {
    case SET_RPS:
      return action.rps;

    case RP_FETCHED:
      const index = state.findIndex(item => item.id === action.rp.id);

      if (index > -1) {
        return state.map(item => {
          if (item.realm === action.rp.id) return action.id;
          return item;
        });
      } else {
        return [
          ...state,
          action.rp
        ];
      }

    case RP_UPDATED:
      return state.map(item => {
        if (item.id === action.rp.id) return action.rp;
        return item;
      })

    case RP_DELETED:
      return state.filter(item => item.id !== action.id);

    case ADD_RP:
      return [
        ...state,
        action.rp
      ];

    default: return state;
  }
}
