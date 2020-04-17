import config from './Config';

export const SET_RPS = 'SET_RPS';
export const ADD_RP = 'ADD_RP';
export const RP_FETCHED = 'RP_FETCHED';
export const RP_UPDATED = 'RP_UPDATED';
export const RP_DELETED = 'RP_DELETED';

function handleResponse(response) {
    if (response.ok) {
        return response.json();
    } else {
        let error = new Error(response.statusText);
        error.response = response;
        throw error;
    }
}

export function fetchRPs() {
    return dispatch => {
        // Get the user's access token
        return window.msal.acquireTokenSilent({
            scopes: config.apiScopes
        }).then(response => {
            return fetch(config.managementApiUri + "/api/relyingparty", {
                headers: {
                    "authorization": "bearer " + response.accessToken
                }
            })
            .then(response => response.json())
            .then(json => {
                dispatch(setRPs(json));
            });
        });
    }
}

export function fetchRP(id) {
    return dispatch => {
        // Get the user's access token
        return window.msal.acquireTokenSilent({
            scopes: config.apiScopes
        }).then(response => {
            return fetch(config.managementApiUri + `/api/relyingparty/${id}`, {
                headers: {
                    "authorization": `bearer ${response.accessToken}`
                }
            })
                .then(response => response.json())
                .then(json => {
                    dispatch(rpFetched(json));
                });
        });
    }
}

export function saveRP(rp) {
    return dispatch => {
        // Get the user's access token
        return window.msal.acquireTokenSilent({
            scopes: config.apiScopes
        }).then(response => {
            return fetch(config.managementApiUri + '/api/relyingparty/', {
                method: 'POST',
                body: JSON.stringify(rp),
                headers: {
                    'Content-Type': 'application/json',
                    'Authorization': `bearer ${response.accessToken}`
                }
            })
            .then(handleResponse)
            .then(json => {
                dispatch(addRP(json));
            });
        });
    }
}

export function updateRP(rp) {
    return dispatch => {

        return window.msal.acquireTokenSilent({
            scopes: config.apiScopes
        }).then(response => {
            return fetch(config.managementApiUri + `/api/relyingparty/${rp.id}`, {
                method: 'PUT',
                body: JSON.stringify(rp),
                headers: {
                    'Content-Type': 'application/json',
                    'Authorization': `bearer ${response.accessToken}`
                }
            })
            .then(handleResponse)
            .then(data => {
                dispatch(rpUpdated(rp));
            });
        });
    }
}

export function deleteRP(id) {
    return dispatch => {

        return window.msal.acquireTokenSilent({
            scopes: config.apiScopes
        }).then(response => {
            return fetch(config.managementApiUri + `/api/relyingparty/${id}`, {
                method: 'DELETE',
                headers: {
                    'Authorization': `bearer ${response.accessToken}`
                }
            })
            .then(response => 
            {
                if(response.ok) {
                    dispatch(rpDeleted(id));
                } else {
                    let error = new Error(response.statusText);
                    error.response = response;
                    throw error;
                }
            });
        });
    }
}

export function setRPs(rps) {
    return {
        type: SET_RPS,
        rps
    }
}

export function addRP(rp) {
    return {
        type: ADD_RP,
        rp
    }
}

export function rpFetched(rp) {
    return {
        type: RP_FETCHED,
        rp
    }
}

export function rpUpdated(rp) {
    return {
        type: RP_UPDATED,
        rp
    }
}

export function rpDeleted(id) {
    return {
        type: RP_DELETED,
        id
    }
}
